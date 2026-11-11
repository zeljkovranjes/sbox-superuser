using SboxSuperuser.Core.Moderation;
using SboxSuperuser.Engine.Audit;
using SboxSuperuser.Engine.Authority;
using SboxSuperuser.Engine.Moderation;

namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	private readonly HostPolicyService _policy;
	private readonly HostModerationService _moderation;
	private readonly HostAuditService _audit;
	private readonly Func<IReadOnlyList<CommandPlayer>> _players;
	private readonly Func<IReadOnlyList<ModuleStatus>> _modules;
	private readonly Func<IReadOnlyList<CommandDescriptor>> _commands;
	private readonly Func<IReadOnlyDictionary<string, string>> _diagnostics;
	private readonly Core.Chat.RecentChat _chat;
	private readonly Commands.CommandRegistry _registry;
	private readonly Alts.HostAltService _alts;
	private static readonly AdminAction[] AllActions = Enum.GetValues<AdminAction>();

	// The server-view providers are optional so hosts and tests that only need player views stay unchanged.
	internal HostAdminQueries( HostPolicyService policy, HostModerationService moderation, HostAuditService audit,
		Func<IReadOnlyList<CommandPlayer>> players, Func<IReadOnlyList<ModuleStatus>> modules = null,
		Func<IReadOnlyList<CommandDescriptor>> commands = null, Func<IReadOnlyDictionary<string, string>> diagnostics = null,
		Core.Chat.RecentChat chat = null, Commands.CommandRegistry registry = null, Alts.HostAltService alts = null )
	{
		_alts = alts;
		_policy = policy;
		_moderation = moderation;
		_audit = audit;
		_players = players;
		_modules = modules ?? (() => Array.Empty<ModuleStatus>());
		_commands = commands ?? (() => Array.Empty<CommandDescriptor>());
		_diagnostics = diagnostics ?? (() => new Dictionary<string, string>());
		_chat = chat ?? new Core.Chat.RecentChat();
		_registry = registry;
	}

	internal async Task<AdminReply> QueryAsync( Func<ActorIdentity> authenticate, AdminQuery query, Guid id, CancellationToken cancellation )
	{
		var initial = authenticate();
		if ( initial is null ) return Failure( id, CommandStatus.Denied, "A live authenticated player is required." );
		await _policy.Initialization;
		await _moderation.Initialization;
		ActorIdentity Current()
		{
			var actor = authenticate();
			return actor?.Kind == initial.Kind && actor?.SubjectId == initial.SubjectId ? actor : null;
		}
		var actor = Current();
		if ( actor is null ) return Failure( id, CommandStatus.Denied, "The requesting connection is no longer valid." );
		if ( !_policy.IsUsable ) return Failure( id, CommandStatus.StorageUnavailable, "Authority data is unavailable." );
		bool Can( string permission ) => _policy.Explain( actor, permission ).IsAllowed;
		switch ( query.Kind )
		{
			case AdminQueryKind.About: return ReadAbout( id, actor );
			case AdminQueryKind.Commands: return ReadCommands( id, actor, Can );
			case AdminQueryKind.Permissions: return ReadPermissions( id, actor );
			case AdminQueryKind.Chat: return ReadChat( id, query, Can );
			case AdminQueryKind.Documentation: return ReadDocumentation( id, query, actor, Can );
			case AdminQueryKind.MenuWindows: return ReadMenuWindows( id, actor, Can );
			case AdminQueryKind.Alts:
			case AdminQueryKind.AltSuspects: return ReadAlts( id, query, Can );
			case AdminQueryKind.Activity:
			case AdminQueryKind.ActivitySearch: return await ReadActivityAsync( id, query, Current, cancellation );
		}
		var actions = AllActions.Where( action => Can( Permission( action ) ) ).ToArray();
		var history = Can( "admin.players.history" );
		var moduleRestrictions = ReadModuleRestrictions( Can, true );
		if ( actions.Length == 0 && !history && moduleRestrictions.Actions.Length == 0 && !moduleRestrictions.CanRead ) return Failure( id, CommandStatus.Denied, "Moderation permission is required to inspect players." );
		var online = _players();
		if ( online is null ) return Failure( id, CommandStatus.Unsupported, "The online player list is unavailable." );
		if ( query.Kind == AdminQueryKind.Restrictions ) return ReadRestrictions( id, query, actions, history, moduleRestrictions.Rows );
		if ( query.Kind == AdminQueryKind.Players )
		{
			var selected = online.Where( player => player.SteamId.Contains( query.Search, StringComparison.Ordinal )
				|| player.DisplayName.Contains( query.Search, StringComparison.OrdinalIgnoreCase ) )
				.OrderBy( player => player.SteamId, StringComparer.Ordinal ).Skip( query.Page * 20 ).Take( 21 ).ToArray();
			return new AdminReply
			{
				RequestId = id, Status = CommandStatus.Success, Message = "Online players.", HasMore = selected.Length > 20,
				ModuleActions = moduleRestrictions.Actions,
				Players = selected.Take( 20 ).Select( player => new AdminPlayerRow
				{
					SteamId = player.SteamId, Name = player.DisplayName,
					Warnings = history ? _moderation.CountWarnings( player.SteamId ) : 0
				} ).ToArray()
			};
		}
		if ( !_moderation.IsUsable ) return Failure( id, CommandStatus.StorageUnavailable, "Moderation data is unavailable." );
		var isOnline = online.Any( player => player.SteamId == query.Search );
		var canTarget = _policy.CheckTarget( actor, query.Search ).IsAllowed;
		var selectedRestrictions = ReadModuleRestrictions( Can, canTarget, query.Search );
		var rows = Array.Empty<AdminModerationRow>();
		var more = false;
		if ( history ) rows = _moderation.ReadView( query.Search, true, query.Page, _ => true, out more );
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = isOnline ? "Connected player." : "Offline Steam ID.",
			SteamId = query.Search, Online = isOnline, Roles = _policy.ReadRoleLabels( query.Search ),
			Actions = actions.Where( action => (action.IsRevocation() || canTarget)
				&& (action != AdminAction.Kick || isOnline) ).ToArray(),
			ModuleActions = selectedRestrictions.Actions,
			HistoryAllowed = history, History = rows, HasMore = more,
			Restrictions = _moderation.ReadView( query.Search, false, 0, Visible( actions, history ), out _ ).Concat( selectedRestrictions.Rows ).ToArray()
		};
	}

	private AdminReply ReadRestrictions( Guid id, AdminQuery query, AdminAction[] actions, bool history, AdminModerationRow[] moduleRows )
	{
		if ( !_moderation.IsUsable ) return Failure( id, CommandStatus.StorageUnavailable, "Moderation data is unavailable." );
		var all = _moderation.ReadAllActive( Visible( actions, history ), query.Search ).Concat( moduleRows.Where( row => row.SteamId.Contains( query.Search, StringComparison.Ordinal ) ) )
			.OrderByDescending( row => row.CreatedUtc ).Skip( query.Page * 20 ).Take( 21 ).ToArray();
		var more = all.Length > 20;
		var rows = all.Take( 20 ).ToArray();
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "Active restrictions.", Restrictions = rows, HasMore = more,
			Actions = actions.Where( action => action.IsRevocation() ).ToArray()
		};
	}

	/// <summary>A restriction is visible to history readers and to anyone who may impose or lift it.</summary>
	private static Func<ModerationKind, bool> Visible( AdminAction[] actions, bool history ) => kind => history || kind switch
	{
		ModerationKind.Ban => actions.Contains( AdminAction.Ban ) || actions.Contains( AdminAction.Unban ),
		ModerationKind.TextMute => actions.Contains( AdminAction.Mute ) || actions.Contains( AdminAction.Unmute ),
		ModerationKind.Gag => actions.Contains( AdminAction.Gag ) || actions.Contains( AdminAction.Ungag ),
		ModerationKind.Gimp => actions.Contains( AdminAction.Gimp ) || actions.Contains( AdminAction.Ungimp ),
		_ => false
	};

	internal static string Permission( AdminAction action ) => "admin.players." + action.ToString().ToLowerInvariant();

	internal static AdminReply FromResult( Guid id, CommandResult result ) => new()
	{
		RequestId = id, Status = result.Status, Message = result.Message,
		Data = result.Data.ToDictionary( pair => pair.Key, pair => pair.Value )
	};

	internal static AdminReply Failure( Guid id, CommandStatus status, string message ) => new()
	{
		RequestId = id, Status = status, Message = message
	};
}
