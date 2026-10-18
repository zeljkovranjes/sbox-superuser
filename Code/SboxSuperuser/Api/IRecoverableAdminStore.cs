namespace SboxSuperuser;

/// <summary>Trusted host-only storage and explicit backup recovery; never expose this interface through a player RPC.</summary>
public interface IRecoverableAdminStore : IAdminStore
{
	/// <summary>Inspects current health and up to 100 compatible committed backups, newest first.</summary>
	Task<StoreBackupList> GetBackupsAsync( StoreKey key, CancellationToken cancellation );

	/// <summary>Restores a selected backup as a new revision with a recovery intent; refuses a changed head.</summary>
	Task<StoreWriteResult> RecoverAsync( StoreKey key, Guid generationId, long expectedHeadRevision,
		AuditIntent recoveryIntent, CancellationToken cancellation );
}
