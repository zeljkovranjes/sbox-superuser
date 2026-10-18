using SboxSuperuser.Core.Audit;

namespace SboxSuperuser.Engine.Audit;

internal sealed partial class HostAuditService
{
	public Task<bool> BeginAsync( AuditAction action, CancellationToken cancellation )
	{
		return SerializedAsync( async () =>
		{
			if ( !await PrepareWriteAsync( cancellation ) )
			{
				return false;
			}
			var protectedIds = new HashSet<Guid>( _active ) { action.Id };
			var candidate = _journal.Append( action.Record( _journal.NextSequence ) ).Trim( _clock(), protectedIds );
			if ( !await PersistAsync( candidate, action, cancellation ) || _retentionUnavailable )
			{
				return false;
			}
			_active.Add( action.Id );
			return true;
		}, _ => false, cancellation );
	}

	public Task<bool> CompleteAsync( AuditAction action, CommandStatus outcome, CancellationToken cancellation )
	{
		return SerializedAsync( async () =>
		{
			try
			{
				if ( !await LoadAsync( cancellation ) )
				{
					return false;
				}
				var protectedIds = new HashSet<Guid>( _active ) { action.Id };
				var candidate = _journal.Complete( action.Id, outcome.ToString(), _clock() ).Trim( _clock(), protectedIds );
				return await PersistAsync( candidate, action, cancellation );
			}
			finally
			{
				_active.Remove( action.Id );
			}
		}, _ => false, cancellation, completion: true );
	}

	public Task<bool> RecordAsync( AuditAction action, CommandStatus outcome, CancellationToken cancellation )
	{
		return SerializedAsync( async () =>
		{
			if ( !await PrepareWriteAsync( cancellation ) )
			{
				return false;
			}
			var protectedIds = new HashSet<Guid>( _active ) { action.Id };
			var candidate = _journal.Append( action.Record( _journal.NextSequence, outcome, _clock() ) ).Trim( _clock(), protectedIds );
			return await PersistAsync( candidate, action, cancellation );
		}, _ => false, cancellation );
	}

	private async Task<bool> PrepareWriteAsync( CancellationToken cancellation )
	{
		if ( !await LoadAsync( cancellation ) || _retention is null )
		{
			_writeFailed = true;
			return false;
		}

		if ( _retentionUnavailable )
		{
			await PruneAsync( cancellation );
		}

		return !_retentionUnavailable;
	}

	private async Task<bool> PersistAsync( AuditJournal candidate, AuditAction action, CancellationToken cancellation )
	{
		var retained = candidate.Records.Select( record => record.Id ).ToHashSet();
		var trimmed = _journal.Records.Any( record => !retained.Contains( record.Id ) );
		var needsCheckpoint = trimmed || _journal.NeedsRetentionCheckpoint;
		if ( !await WriteOnceAsync( candidate.WithRetentionCheckpoint( needsCheckpoint ), action, cancellation ) )
		{
			return false;
		}
		// A second compacted snapshot ensures the retained backup cannot carry expired records.
		if ( needsCheckpoint && !await WriteOnceAsync( candidate.WithRetentionCheckpoint( false ), action, cancellation ) )
		{
			_retentionPending = true;
			return false;
		}

		await PruneAsync( CancellationToken.None );
		return true;
	}

	private async Task<bool> WriteOnceAsync( AuditJournal candidate, AuditAction action, CancellationToken cancellation )
	{
		var transaction = new PolicyTransaction( AuditFormat.WriteState( candidate ), action.Intents );
		var result = await _store.WriteAsync( transaction.CreateWriteRequest( Key, _revision ), cancellation );
		_writeStatus = result.Status;
		_writeFailed = result.Status != StoreWriteStatus.Committed;
		if ( _writeFailed )
		{
			await LoadAsync( CancellationToken.None );
			return false;
		}

		_journal = candidate;
		_revision = result.CommittedRevision.Value;
		_readStatus = StoreReadStatus.Found;
		return true;
	}
}
