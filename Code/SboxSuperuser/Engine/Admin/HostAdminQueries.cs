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
	private static readonly AdminAction[] AllActions = Enum.GetValues<AdminAction>();

	// The server-view providers are optional so hosts and tests that only need player views stay unchanged.
	internal HostAdminQueries( HostPolicyService policy, HostModerationService moderation, HostAuditService audit,
		Func<IReadOnlyList<CommandPlayer>> players, Func<IReadOnlyList<ModuleStatus>> modules = null,
		Func<IReadOnlyList<CommandDescriptor>> commands = null, Func<IReadOnlyDictionary<string, string>> diagnostics = null,
		Core.Chat.RecentChat chat = null, Commands.CommandRegistry registry = null )
	{
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
			case AdminQueryKind.Activity:
			case AdminQueryKind.ActivitySearch: return await ReadActivityAsync( id, query, Current, cancellation );
		}
		var actions = AllActions.Where( action => Can( Permission( action ) ) ).ToArray();
		var history = Can( "admin.players.history" );
		if ( actions.Length == 0 && !history ) return Failure( id, CommandStatus.Denied, "Moderation permission is required to inspect players." );
		var online = _players();
		if ( online is null ) return Failure( id, CommandStatus.Unsupported, "The online player list is unavailable." );
		if ( query.Kind == AdminQueryKind.Restrictions ) return ReadRestrictions( id, query, actions, history );
		if ( query.Kind == AdminQueryKind.Players )
		{
			var selected = online.Where( player => player.SteamId.Contains( query.Search, StringComparison.Ordinal )
				|| player.DisplayName.Contains( query.Search, StringComparison.OrdinalIgnoreCase ) )
				.OrderBy( player => player.SteamId, StringComparer.Ordinal ).Skip( query.Page * 20 ).Take( 21 ).ToArray();
			return new AdminReply
			{
				RequestId = id, Status = CommandStatus.Success, Message = "Online players.", HasMore = selected.Length > 20,
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
		var rows = Array.Empty<AdminModerationRow>();
		var more = false;
		if ( history ) rows = _moderation.ReadView( query.Search, true, query.Page, true, true, out more );
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = isOnline ? "Connected player." : "Offline Steam ID.",
			SteamId = query.Search, Online = isOnline, Roles = _policy.ReadRoleLabels( query.Search ),
			Actions = actions.Where( action => (action is AdminAction.Unban or AdminAction.Unmute || canTarget)
				&& (action != AdminAction.Kick || isOnline) ).ToArray(),
			HistoryAllowed = history, History = rows, HasMore = more,
			Restrictions = _moderation.ReadView( query.Search, false, 0,
				history || actions.Contains( AdminAction.Ban ) || actions.Contains( AdminAction.Unban ),
				history || actions.Contains( AdminAction.Mute ) || actions.Contains( AdminAction.Unmute ), out _ )
		};
	}

	private AdminReply ReadRestrictions( Guid id, AdminQuery query, AdminAction[] actions, bool history )
	{
		if ( !_moderation.IsUsable ) return Failure( id, CommandStatus.StorageUnavailable, "Moderation data is unavailable." );
		var bans = history || actions.Contains( AdminAction.Ban ) || actions.Contains( AdminAction.Unban );
		var mutes = history || actions.Contains( AdminAction.Mute ) || actions.Contains( AdminAction.Unmute );
		var rows = _moderation.ReadActive( bans, mutes, query.Search, query.Page, out var more );
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "Active restrictions.", Restrictions = rows, HasMore = more,
			Actions = actions.Where( action => action is AdminAction.Unban or AdminAction.Unmute ).ToArray()
		};
	}

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
