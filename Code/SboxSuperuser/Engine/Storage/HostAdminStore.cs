using Sandbox;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Storage;

internal sealed class HostAdminStore : IRecoverableAdminStore, Audit.IAuditRetention
{
	private readonly StoreCoordinator _coordinator;
	private readonly string _root;
	internal event Action<StoreKey> StorageChanged;

	internal HostAdminStore( bool dedicated, Func<bool> available )
	{
		_root = dedicated ? "superuser/store/dedicated" : "superuser/store/editor";
		bool IsAvailable() => ThreadSafe.IsMainThread && available();
		_coordinator = new StoreCoordinator( new EngineStoreFiles( IsAvailable ), IsAvailable );
	}

	public async Task<StoreReadResult> ReadAsync( StoreKey key, int supportedSchemaVersion, CancellationToken cancellation )
	{
		var result = await _coordinator.ReadAsync( DirectoryFor( key ), supportedSchemaVersion, cancellation );
		Report( key, result.Status );
		return new StoreReadResult( ReadStatus( result.Status ), result.Document is null ? null
			: new VersionedStoreDocument( result.Document.SchemaVersion, result.Document.Revision, result.Document.PayloadJson ) );
	}

	public Task<StoragePruneResult> PruneAuditAsync( long expectedRevision, CancellationToken cancellation )
	{
		// No caller-controlled key: general policy and owner backups never enter automatic retention.
		return _coordinator.PruneAuditHistoryAsync( DirectoryFor( new StoreKey( "audit", "journal" ) ), expectedRevision, cancellation );
	}

	public async Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( request );
		var result = await _coordinator.WriteAsync( DirectoryFor( request.Key ), request.Document.SchemaVersion,
			request.ExpectedRevision, request.Document.PayloadJson, cancellation );
		Report( request.Key, result.Status );
		NotifyChange( request.Key, result.Status );
		return WriteResult( result );
	}

	public async Task<StoreBackupList> GetBackupsAsync( StoreKey key, CancellationToken cancellation )
	{
		var result = await _coordinator.GetBackupsAsync( DirectoryFor( key ), cancellation );
		Report( key, result.Status );
		return new StoreBackupList( ReadStatus( result.Status ), result.HeadRevision,
			result.Documents.Select( document => new StoreBackup( document.GenerationId, document.Revision, document.SchemaVersion ) ), result.HasMore );
	}

	public async Task<StoreWriteResult> RecoverAsync( StoreKey key, Guid generationId, long expectedHeadRevision,
		AuditIntent recoveryIntent, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( recoveryIntent );
		if ( generationId == Guid.Empty || expectedHeadRevision < 1 )
		{
			throw new ArgumentException( "Recovery requires an explicit generation and positive observed head revision." );
		}

		var result = await _coordinator.RecoverAsync( DirectoryFor( key ), generationId, expectedHeadRevision,
			recoveryIntent.ToJson(), cancellation );
		Report( key, result.Status );
		NotifyChange( key, result.Status );
		return WriteResult( result );
	}

	private void NotifyChange( StoreKey key, StorageStatus status )
	{
		if ( status is StorageStatus.Committed or StorageStatus.Unknown or StorageStatus.Corrupt or StorageStatus.Conflict )
		{
			StorageChanged?.Invoke( key );
		}
	}

	private string DirectoryFor( StoreKey key )
	{
		ArgumentNullException.ThrowIfNull( key );
		return $"{_root}/{key.ModuleId}/{key.DocumentId}";
	}

	private static StoreReadStatus ReadStatus( StorageStatus status ) => status switch
	{
		StorageStatus.Found => StoreReadStatus.Found,
		StorageStatus.Missing => StoreReadStatus.Missing,
		StorageStatus.Corrupt => StoreReadStatus.Corrupt,
		StorageStatus.UnsupportedSchema => StoreReadStatus.UnsupportedSchema,
		StorageStatus.Unavailable => StoreReadStatus.Unavailable,
		StorageStatus.Cancelled => StoreReadStatus.Cancelled,
		_ => StoreReadStatus.Unknown
	};

	private static StoreWriteResult WriteResult( StorageOutcome outcome )
	{
		var status = outcome.Status switch
		{
			StorageStatus.Committed => StoreWriteStatus.Committed,
			StorageStatus.Conflict => StoreWriteStatus.Conflict,
			StorageStatus.UnsupportedSchema => StoreWriteStatus.UnsupportedSchema,
			StorageStatus.Unavailable => StoreWriteStatus.Unavailable,
			StorageStatus.Cancelled => StoreWriteStatus.Cancelled,
			StorageStatus.InvalidData => StoreWriteStatus.InvalidData,
			StorageStatus.Corrupt => StoreWriteStatus.Corrupt,
			_ => StoreWriteStatus.Unknown
		};
		return new StoreWriteResult( status, status == StoreWriteStatus.Committed ? outcome.Document.Revision : null );
	}

	private static void Report( StoreKey key, StorageStatus status )
	{
		if ( status is StorageStatus.Corrupt or StorageStatus.UnsupportedSchema or StorageStatus.Unavailable or StorageStatus.Unknown )
		{
			Log.Warning( $"[Superuser] Storage degraded; key={key.ModuleId}/{key.DocumentId}; status={status}. Existing files preserved." );
		}
	}
}
