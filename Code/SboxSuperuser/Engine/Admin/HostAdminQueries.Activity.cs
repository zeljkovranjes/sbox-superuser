namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	private const int ActivityPageSize = 20;

	// Audit pages chain by cursor, so a later page is reached by following cursors from the newest page.
	private async Task<AdminReply> ReadActivityAsync( Guid id, AdminQuery query, Func<ActorIdentity> authenticate, CancellationToken cancellation )
	{
		AuditCursor cursor = null;
		AuditPage page = null;
		for ( var index = 0; index <= query.Page; index++ )
		{
			if ( index > 0 && cursor is null ) break;
			page = await _audit.QueryAsync( authenticate, who => _policy.Explain( who, "admin.audit.read" ).IsAllowed,
				who => _policy.Explain( who, "admin.results.own" ).IsAllowed, Filter( query, cursor ), cancellation );
			if ( page.Status != CommandStatus.Success ) break;
			cursor = page.NextCursor;
		}

		return new AdminReply
		{
			RequestId = id, Status = page.Status, Message = page.Message,
			HasMore = page.Status == CommandStatus.Success && page.NextCursor is not null,
			Activity = page.Entries.Select( row => new AdminActivityRow
			{
				RequestId = row.RequestId, Actor = row.ActorSubject, Command = row.Command,
				CreatedUtc = row.CreatedUtc, Outcome = row.Outcome
			} ).ToArray()
		};
	}

	private static AuditQuery Filter( AdminQuery query, AuditCursor cursor ) => new(
		pageSize: ActivityPageSize, cursor: cursor,
		command: query.Search.Length == 0 ? null : query.Search,
		actorSubject: query.Actor.Length == 0 ? null : query.Actor,
		outcome: query.Outcome );
}
