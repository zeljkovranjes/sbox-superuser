namespace SboxSuperuser;

/// <summary>Current read health and compatible backups; Corrupt may still have valid recovery candidates.</summary>
public sealed class StoreBackupList
{
	/// <summary>Gets the current document's read status, not a claim that every retained file is healthy.</summary>
	public StoreReadStatus Status { get; }

	/// <summary>Gets the highest observed file revision, including incomplete writes, for recovery conflict detection.</summary>
	public long HeadRevision { get; }

	/// <summary>Gets immutable compatible backup metadata in newest-first order.</summary>
	public IReadOnlyList<StoreBackup> Backups { get; }

	/// <summary>Gets whether more than 100 compatible backups exist.</summary>
	public bool HasMore { get; }

	/// <summary>Creates a bounded backup inspection result for a custom backend.</summary>
	public StoreBackupList( StoreReadStatus status, long headRevision, IEnumerable<StoreBackup> backups, bool hasMore )
	{
		Status = Core.Contracts.ContractGuard.EnumValue( status, nameof( status ) );
		if ( headRevision < 0 ) throw new ArgumentOutOfRangeException( nameof( headRevision ) );
		var copy = Core.Contracts.ContractGuard.Copy( backups, nameof( backups ) );
		if ( copy.Count > 100 || copy.Any( backup => backup.Revision > headRevision ) )
			throw new ArgumentException( "At most 100 backups at or before the observed head are supported." );
		HeadRevision = headRevision;
		Backups = copy;
		HasMore = hasMore;
	}
}
