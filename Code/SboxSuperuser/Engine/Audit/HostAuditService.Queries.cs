using SboxSuperuser.Core.Audit;

namespace SboxSuperuser.Engine.Audit;

internal sealed partial class HostAuditService
{
	internal Task<AuditPage> QueryAsync( Func<ActorIdentity> authenticate, Func<ActorIdentity, bool> canReadAll,
		Func<ActorIdentity, bool> canReadOwn, AuditQuery query, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( query );
		return SerializedAsync( async () =>
		{
			var initial = authenticate();
			if ( initial is null || !canReadAll( initial ) && !canReadOwn( initial ) )
			{
				return new AuditPage( CommandStatus.Denied, "Audit access denied." );
			}

			if ( !await LoadAsync( cancellation ) )
			{
				return new AuditPage( CommandStatus.StorageUnavailable, "Audit storage is unavailable; existing evidence is preserved." );
			}

			var actor = authenticate();
			if ( actor is null || actor.Kind != initial.Kind || actor.SubjectId != initial.SubjectId )
			{
				return new AuditPage( CommandStatus.Denied, "Audit reader identity is no longer valid." );
			}

			var all = canReadAll( actor );
			if ( !all && (!canReadOwn( actor ) || actor.Kind != ActorKind.Player
				|| query.ActorSubject is not null && query.ActorSubject != actor.SubjectId) )
			{
				return new AuditPage( CommandStatus.Denied, "This caller may read only its own audit results." );
			}

			if ( query.Cursor is not null && query.Cursor.Revision != _revision )
			{
				return new AuditPage( CommandStatus.Conflict, "The audit journal changed; restart pagination." );
			}

			var now = _clock();
			var matches = _journal.Records.Where( record => (all || record.ActorKind == "Player" && record.ActorSubject == actor.SubjectId)
				&& (!_journal.IsExpired( record, now ) || _active.Contains( record.Id )) && Matches( record, query ) )
				.OrderByDescending( record => record.Sequence ).Take( query.PageSize + 1 ).ToArray();
			var selected = matches.Take( query.PageSize ).ToArray();
			var cursor = matches.Length > query.PageSize ? new AuditCursor( _revision, selected[ selected.Length - 1 ].Sequence ) : null;
			return new AuditPage( CommandStatus.Success, "Audit page read.", _revision, selected.Select( record => new AuditEntry( record ) ), cursor );
		}, status => new AuditPage( status, "Audit query could not complete." ), cancellation );
	}

	internal Task<AuditDiagnostics> ReadDiagnosticsAsync( Func<bool> authorize, CancellationToken cancellation )
	{
		return SerializedAsync( async () =>
		{
			if ( !authorize() )
			{
				return null;
			}

			await LoadAsync( cancellation );
			return authorize() ? Diagnostics() : null;
		}, _ => (AuditDiagnostics)null, cancellation );
	}

	private static bool Matches( AuditRecord record, AuditQuery query )
	{
		return (query.Cursor is null || record.Sequence < query.Cursor.BeforeSequence)
			&& (!query.RequestId.HasValue || record.RequestId == query.RequestId.Value)
			&& (query.Command is null || record.Command == query.Command)
			&& (query.ActorSubject is null || record.ActorSubject == query.ActorSubject)
			&& (!query.Outcome.HasValue || record.Outcome == query.Outcome.Value.ToString())
			&& (!query.Origin.HasValue || record.Origin == query.Origin.Value.ToString())
			&& (!query.FromUtc.HasValue || record.CreatedUtc >= query.FromUtc.Value)
			&& (!query.ToUtc.HasValue || record.CreatedUtc <= query.ToUtc.Value)
			&& (query.TargetSteamId is null || record.Targets.Contains( query.TargetSteamId, StringComparer.Ordinal ));
	}
}
