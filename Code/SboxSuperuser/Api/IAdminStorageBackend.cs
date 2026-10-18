namespace SboxSuperuser;

/// <summary>Replaceable host storage, including recovery and audit-generation retention; implement entirely with s&amp;box-compatible APIs.</summary>
public interface IAdminStorageBackend : IRecoverableAdminStore
{
	/// <summary>Removes superseded audit generations at the expected head while preserving the current journal and recoverability.</summary>
	Task<StoreRetentionResult> PruneAuditHistoryAsync( long expectedRevision, CancellationToken cancellation );
}
