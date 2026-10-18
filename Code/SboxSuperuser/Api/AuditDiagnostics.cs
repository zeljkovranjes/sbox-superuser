namespace SboxSuperuser;

/// <summary>Host-local audit health without record contents or argument values.</summary>
public sealed class AuditDiagnostics
{
	/// <summary>Gets whether persistence or retention needs operator attention.</summary>
	public bool IsDegraded { get; }
	/// <summary>Gets the most recent validated read status.</summary>
	public StoreReadStatus ReadStatus { get; }
	/// <summary>Gets the last audit write status; Unknown also represents no write yet.</summary>
	public StoreWriteStatus LastWriteStatus { get; }
	/// <summary>Gets whether old audit files still await bounded cleanup.</summary>
	public bool RetentionPending { get; }
	/// <summary>Gets the active committed journal revision.</summary>
	public long Revision { get; }
	/// <summary>Gets retained journal rows.</summary>
	public int RetainedCount { get; }
	/// <summary>Gets the configured retention count and age.</summary>
	public AuditRetentionOptions Retention { get; }

	internal AuditDiagnostics( bool degraded, StoreReadStatus readStatus, StoreWriteStatus writeStatus, bool retentionPending,
		long revision, int retainedCount, AuditRetentionOptions retention )
	{
		IsDegraded = degraded;
		ReadStatus = readStatus;
		LastWriteStatus = writeStatus;
		RetentionPending = retentionPending;
		Revision = revision;
		RetainedCount = retainedCount;
		Retention = retention;
	}
}
