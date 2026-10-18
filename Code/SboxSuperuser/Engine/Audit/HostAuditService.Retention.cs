using SboxSuperuser.Core.Audit;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Audit;

internal sealed partial class HostAuditService
{
	internal Task<bool> MaintainAutomaticAsync( CancellationToken cancellation )
	{
		return SerializedAsync( async () =>
		{
			if ( !await LoadAsync( cancellation ) )
			{
				return false;
			}
			var candidate = _journal.Trim( _clock(), _active );
			if ( candidate.Records.Count != _journal.Records.Count || _journal.NeedsRetentionCheckpoint )
			{
				var actor = new ActorIdentity( ActorKind.InternalService, "audit.retention" );
				var action = new AuditAction( actor, CommandOrigin.InternalService, Guid.NewGuid(), "audit.retention", _clock() );
				return await PersistAsync( candidate, action, cancellation );
			}

			await PruneAsync( cancellation );
			return !_retentionFailed;
		}, _ => false, cancellation );
	}

	internal Task<CommandResult> ConfigureAsync( Func<ActorIdentity> authorizeOwner, AuditRetentionOptions options, CancellationToken cancellation )
	{
		return SerializedAsync( async () =>
		{
			if ( authorizeOwner() is null )
			{
				return new CommandResult( CommandStatus.Denied, "Only a current owner or dedicated local console may configure audit retention." );
			}

			if ( !await PrepareWriteAsync( cancellation ) )
			{
				return new CommandResult( CommandStatus.StorageUnavailable, "Audit storage is unavailable." );
			}
			var actor = authorizeOwner();
			if ( actor is null )
			{
				return new CommandResult( CommandStatus.Denied, "Only a current owner or dedicated local console may configure audit retention." );
			}
			var action = new AuditAction( actor, CommandOrigin.Console,
				Guid.NewGuid(), "audit.retention.configure", _clock(), new Dictionary<string, string>
				{
					[ "maximumrecords" ] = options.MaximumRecords.ToString( System.Globalization.CultureInfo.InvariantCulture ),
					[ "retentiondays" ] = options.RetentionDays.ToString( System.Globalization.CultureInfo.InvariantCulture )
				} );
			var candidate = new AuditJournal( options.MaximumRecords, options.RetentionDays, _journal.NextSequence, _journal.Records, _journal.NeedsRetentionCheckpoint )
				.Append( action.Record( _journal.NextSequence, CommandStatus.Success, _clock() ) )
				.Trim( _clock(), new HashSet<Guid>( _active ) { action.Id } );
			return await PersistAsync( candidate, action, cancellation )
				? new CommandResult( CommandStatus.Success, "Audit retention persisted; diagnostics report any remaining cleanup." )
				: new CommandResult( CommandStatus.Unknown, "Audit retention did not reach a confirmed outcome." );
		}, status => new CommandResult( status, "Audit retention could not be changed." ), cancellation );
	}

	private async Task PruneAsync( CancellationToken cancellation )
	{
		if ( _retention is null )
		{
			_retentionUnavailable = _retentionFailed = _retentionPending = true;
			return;
		}

		var result = await _retention.PruneAuditAsync( _revision, cancellation );
		_retentionPending = result.HasMore;
		_retentionFailed = result.Status != StorageStatus.Committed;
		_retentionUnavailable = result.Status is StorageStatus.Unavailable or StorageStatus.Unknown or StorageStatus.Cancelled or StorageStatus.Conflict;
	}
}
