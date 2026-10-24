namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	private const int ActivityPageSize = 20;

	private async Task<AdminReply> ReadActivityAsync( Guid id, AdminQuery query, Func<ActorIdentity> authenticate, CancellationToken cancellation )
	{
		var page = await _audit.QueryPageAsync( authenticate, who => _policy.Explain( who, "admin.audit.read" ).IsAllowed,
			who => _policy.Explain( who, "admin.results.own" ).IsAllowed, Filter( query ), query.Page, cancellation );

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

	private static AuditQuery Filter( AdminQuery query ) => new(
		pageSize: ActivityPageSize,
		command: query.Search.Length == 0 ? null : query.Search,
		actorSubject: query.Actor.Length == 0 ? null : query.Actor,
		outcome: query.Outcome );
}
