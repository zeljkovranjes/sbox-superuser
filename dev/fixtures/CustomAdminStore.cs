using Sandbox;
using SboxSuperuser;

namespace SuperuserExamples.Engine;

/// <summary>A working storage decorator; replace delegation with your host database implementation.</summary>
public sealed class CustomAdminStore : IAdminStorageBackend
{
	private readonly IAdminStorageBackend _inner;

	/// <summary>Wraps the selected backend without changing its durability guarantees.</summary>
	public CustomAdminStore( IAdminStorageBackend inner ) => _inner = inner ?? throw new ArgumentNullException( nameof( inner ) );

	/// <summary>Reads a versioned document.</summary>
	public Task<StoreReadResult> ReadAsync( StoreKey key, int schema, CancellationToken cancellation )
		=> _inner.ReadAsync( key, schema, cancellation );

	/// <summary>Writes the aggregate and reports failures without logging private payloads.</summary>
	public async Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation )
	{
		var result = await _inner.WriteAsync( request, cancellation );
		if ( result.Status != StoreWriteStatus.Committed ) Log.Warning( $"Storage write: {result.Status}" );
		return result;
	}

	/// <summary>Lists recoverable generations.</summary>
	public Task<StoreBackupList> GetBackupsAsync( StoreKey key, CancellationToken cancellation )
		=> _inner.GetBackupsAsync( key, cancellation );

	/// <summary>Restores a backup as a new audited revision.</summary>
	public Task<StoreWriteResult> RecoverAsync( StoreKey key, Guid generationId, long expectedHeadRevision,
		AuditIntent intent, CancellationToken cancellation )
		=> _inner.RecoverAsync( key, generationId, expectedHeadRevision, intent, cancellation );

	/// <summary>Prunes only superseded audit generations.</summary>
	public Task<StoreRetentionResult> PruneAuditHistoryAsync( long expectedRevision, CancellationToken cancellation )
		=> _inner.PruneAuditHistoryAsync( expectedRevision, cancellation );
}
