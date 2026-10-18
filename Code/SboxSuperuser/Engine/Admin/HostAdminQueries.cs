using SboxSuperuser.Engine.Audit;
using SboxSuperuser.Engine.Authority;
using SboxSuperuser.Engine.Moderation;

namespace SboxSuperuser.Engine.Admin;

internal sealed class HostAdminQueries
{
	private readonly HostPolicyService _policy;
	private readonly HostModerationService _moderation;
	private readonly HostAuditService _audit;
	private readonly Func<IReadOnlyList<CommandPlayer>> _players;
	private static readonly AdminAction[] AllActions = Enum.GetValues<AdminAction>();

	internal HostAdminQueries( HostPolicyService policy, HostModerationService moderation, HostAuditService audit,
		Func<IReadOnlyList<CommandPlayer>> players )
	{
		_policy = policy;
		_moderation = moderation;
		_audit = audit;
		_players = players;
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
		if ( query.Kind == AdminQueryKind.Activity )
		{
			var result = await _audit.QueryAsync( Current, who => _policy.Explain( who, "admin.audit.read" ).IsAllowed,
				who => _policy.Explain( who, "admin.results.own" ).IsAllowed, new AuditQuery( pageSize: 20 ), cancellation );
			return new AdminReply
			{
				RequestId = id, Status = result.Status, Message = result.Message,
				Activity = result.Entries.Select( row => new AdminActivityRow
				{
					RequestId = row.RequestId, Actor = row.ActorSubject, Command = row.Command,
					CreatedUtc = row.CreatedUtc, Outcome = row.Outcome
				} ).ToArray()
			};
		}
		var actions = AllActions.Where( action => Can( Permission( action ) ) ).ToArray();
		var history = Can( "admin.players.history" );
		if ( actions.Length == 0 && !history ) return Failure( id, CommandStatus.Denied, "Moderation permission is required to inspect players." );
		var online = _players();
		if ( online is null ) return Failure( id, CommandStatus.Unsupported, "The online player list is unavailable." );
		if ( query.Kind == AdminQueryKind.Players )
		{
			var selected = online.Where( player => player.SteamId.Contains( query.Search, StringComparison.Ordinal )
				|| player.DisplayName.Contains( query.Search, StringComparison.OrdinalIgnoreCase ) )
				.OrderBy( player => player.SteamId, StringComparer.Ordinal ).Skip( query.Page * 20 ).Take( 21 ).ToArray();
			return new AdminReply
			{
				RequestId = id, Status = CommandStatus.Success, Message = "Online players.", HasMore = selected.Length > 20,
				Players = selected.Take( 20 ).Select( player => new AdminPlayerRow { SteamId = player.SteamId, Name = player.DisplayName } ).ToArray()
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

	internal static string Permission( AdminAction action ) => "admin.players." + action.ToString().ToLowerInvariant();

	internal static AdminReply Failure( Guid id, CommandStatus status, string message ) => new()
	{
		RequestId = id, Status = status, Message = message
	};
}
