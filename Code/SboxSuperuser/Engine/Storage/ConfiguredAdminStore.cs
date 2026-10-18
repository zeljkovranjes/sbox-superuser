using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Storage;

internal sealed class ConfiguredAdminStore : IRecoverableAdminStore, Audit.IAuditRetention
{
	private readonly IAdminStorageBackend _backend;
	private readonly Func<bool> _available;
	private readonly Action<Exception> _report;
	internal event Action<StoreKey> StorageChanged;

	internal ConfiguredAdminStore( IAdminStorageBackend backend, Func<bool> available, Action<Exception> report )
	{
		_backend = backend;
		_available = available;
		_report = report;
	}

	private bool Available => _backend is not null && _available();

	public async Task<StoreReadResult> ReadAsync( StoreKey key, int supportedSchemaVersion, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( key );
		if ( !Available ) return new( StoreReadStatus.Unavailable );
		try
		{
			var result = await _backend.ReadAsync( key, supportedSchemaVersion, cancellation );
			if ( !Available || result is null ) return new( StoreReadStatus.Unavailable );
			if ( result.Status == StoreReadStatus.Found )
			{
				if ( result.Document.SchemaVersion != supportedSchemaVersion ) return new( StoreReadStatus.UnsupportedSchema );
				TransactionFormat.Validate( result.Document.PayloadJson );
			}
			return result;
		}
		catch ( OperationCanceledException ) { return new( StoreReadStatus.Cancelled ); }
		catch ( Exception error )
		{
			_report( error );
			return new( error is FormatException or System.Text.Json.JsonException ? StoreReadStatus.Corrupt : StoreReadStatus.Unavailable );
		}
	}

	public Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( request );
		try { TransactionFormat.Validate( request.Document.PayloadJson ); }
		catch ( Exception error ) when ( error is FormatException or ArgumentException or System.Text.Json.JsonException or InvalidOperationException )
		{
			return Task.FromResult( new StoreWriteResult( StoreWriteStatus.InvalidData ) );
		}
		return MutateAsync( request.Key, request.Document.Revision, () => _backend.WriteAsync( request, cancellation ), cancellation );
	}

	public async Task<StoreBackupList> GetBackupsAsync( StoreKey key, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( key );
		StoreBackupList Unavailable() => new( StoreReadStatus.Unavailable, 0, Array.Empty<StoreBackup>(), false );
		if ( !Available ) return Unavailable();
		try
		{
			var result = await _backend.GetBackupsAsync( key, cancellation );
			return Available && result is not null ? result : Unavailable();
		}
		catch ( Exception error ) { _report( error ); return Unavailable(); }
	}

	public Task<StoreWriteResult> RecoverAsync( StoreKey key, Guid generationId, long expectedHeadRevision,
		AuditIntent recoveryIntent, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( key );
		ArgumentNullException.ThrowIfNull( recoveryIntent );
		if ( generationId == Guid.Empty || expectedHeadRevision is < 1 or long.MaxValue )
			throw new ArgumentException( "Recovery requires an explicit generation and valid observed head." );
		return MutateAsync( key, expectedHeadRevision + 1,
			() => _backend.RecoverAsync( key, generationId, expectedHeadRevision, recoveryIntent, cancellation ), cancellation );
	}

	private async Task<StoreWriteResult> MutateAsync( StoreKey key, long revision, Func<Task<StoreWriteResult>> operation, CancellationToken cancellation )
	{
		if ( !Available ) return new( StoreWriteStatus.Unavailable );
		if ( cancellation.IsCancellationRequested ) return new( StoreWriteStatus.Cancelled );
		StoreWriteResult result;
		try
		{
			result = await operation();
			if ( !Available || result is null || result.Status == StoreWriteStatus.Committed && result.CommittedRevision != revision )
				result = new( StoreWriteStatus.Unknown );
		}
		catch ( Exception error )
		{
			_report( error );
			// Once the provider was invoked, cancellation or an exception cannot prove it did not commit.
			result = new( StoreWriteStatus.Unknown );
		}
		if ( result.Status is StoreWriteStatus.Committed or StoreWriteStatus.Unknown or StoreWriteStatus.Corrupt or StoreWriteStatus.Conflict )
			StorageChanged?.Invoke( key );
		return result;
	}

	public async Task<StoragePruneResult> PruneAuditAsync( long expectedRevision, CancellationToken cancellation )
	{
		if ( !Available ) return new( StorageStatus.Unavailable, 0, true );
		try
		{
			var result = await _backend.PruneAuditHistoryAsync( expectedRevision, cancellation );
			if ( !Available || result is null ) return new( StorageStatus.Unknown, 0, true );
			return new( Enum.Parse<StorageStatus>( result.Status.ToString() ), 0, result.HasMore );
		}
		catch ( Exception error ) { _report( error ); return new( StorageStatus.Unknown, 0, true ); }
	}
}
