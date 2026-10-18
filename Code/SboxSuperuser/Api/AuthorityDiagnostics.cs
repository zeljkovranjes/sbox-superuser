namespace SboxSuperuser;

/// <summary>Host-local security health without exposing owner IDs or stored user policy.</summary>
public sealed class AuthorityDiagnostics
{
	/// <summary>Gets current policy storage health; Missing means validated first-run defaults.</summary>
	public StoreReadStatus PolicyStatus { get; }

	/// <summary>Gets current owner-source health; corruption never falls back to bootstrap owners.</summary>
	public StoreReadStatus OwnerStatus { get; }

	/// <summary>Gets the active policy revision.</summary>
	public long PolicyRevision { get; }

	/// <summary>Gets the managed owner revision, or zero while using bootstrap configuration.</summary>
	public long OwnerRevision { get; }

	/// <summary>Gets the validated owner's count; this snapshot grants no authority.</summary>
	public int OwnerCount { get; }

	/// <summary>Gets bootstrap or managed, identifying the authoritative owner source.</summary>
	public string OwnerSource { get; }

	/// <summary>Gets the last persistent authority mutation's storage result.</summary>
	public StoreWriteStatus LastWriteStatus { get; }

	internal AuthorityDiagnostics( StoreReadStatus policyStatus, StoreReadStatus ownerStatus, long policyRevision,
		long ownerRevision, int ownerCount, string ownerSource, StoreWriteStatus lastWriteStatus )
	{
		PolicyStatus = policyStatus;
		OwnerStatus = ownerStatus;
		PolicyRevision = policyRevision;
		OwnerRevision = ownerRevision;
		OwnerCount = ownerCount;
		OwnerSource = ownerSource;
		LastWriteStatus = lastWriteStatus;
	}
}
