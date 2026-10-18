using SboxSuperuser.Core.Audit;

namespace SboxSuperuser;

/// <summary>Explicit host audit retention; active intents remain protected and the byte ceiling may retain fewer rows.</summary>
public sealed class AuditRetentionOptions
{
	/// <summary>Gets the maximum retained rows, from 1 through 4096; the default is 512.</summary>
	public int MaximumRecords { get; }

	/// <summary>Gets the maximum creation age in days, from 1 through 3650; the default is 30.</summary>
	public int RetentionDays { get; }

	/// <summary>Creates validated retention settings; applying them requires owner authority.</summary>
	public AuditRetentionOptions( int maximumRecords = 512, int retentionDays = 30 )
	{
		_ = new AuditJournal( maximumRecords, retentionDays );
		MaximumRecords = maximumRecords;
		RetentionDays = retentionDays;
	}
}
