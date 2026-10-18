namespace SboxSuperuser;

/// <summary>A pagination position tied to one journal revision; changed journals require a fresh query.</summary>
public sealed class AuditCursor
{
	/// <summary>Gets the revision at which the previous page was read.</summary>
	public long Revision { get; }
	/// <summary>Gets the exclusive upper sequence bound for the next page.</summary>
	public long BeforeSequence { get; }

	/// <summary>Creates a cursor from a previous response; it conveys no permission.</summary>
	public AuditCursor( long revision, long beforeSequence )
	{
		if ( revision < 1 || beforeSequence < 1 )
		{
			throw new ArgumentOutOfRangeException( nameof( revision ), "Audit cursors require positive revision and sequence values." );
		}

		Revision = revision;
		BeforeSequence = beforeSequence;
	}
}
