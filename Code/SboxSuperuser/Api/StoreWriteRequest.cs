namespace SboxSuperuser;

/// <summary>One aggregate replacement with an expected revision; required audit intent belongs in that aggregate.</summary>
public sealed class StoreWriteRequest
{
	/// <summary>Gets the logical host-local destination.</summary>
	public StoreKey Key { get; }

	/// <summary>Gets the candidate generation; the previous readable generation must be retained on failure.</summary>
	public VersionedStoreDocument Document { get; }

	/// <summary>Gets the expected prior revision; zero means create only if missing, never reset corruption.</summary>
	public long ExpectedRevision { get; }

	/// <summary>Creates a candidate request without writing it.</summary>
	public StoreWriteRequest( StoreKey key, VersionedStoreDocument document, long expectedRevision )
	{
		Key = key ?? throw new ArgumentNullException( nameof( key ) );
		Document = document ?? throw new ArgumentNullException( nameof( document ) );
		if ( expectedRevision < 0 || expectedRevision == long.MaxValue
			|| document.Revision != expectedRevision + 1 )
		{
			throw new ArgumentException( "Candidate revision must immediately follow the expected revision.", nameof( expectedRevision ) );
		}

		ExpectedRevision = expectedRevision;
	}
}
