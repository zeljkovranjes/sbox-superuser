namespace SboxSuperuser;

/// <summary>Metadata for a verified backup; does not expose its policy or audit contents.</summary>
public sealed class StoreBackup
{
	/// <summary>Gets the generation selected for explicit recovery.</summary>
	public Guid GenerationId { get; }

	/// <summary>Gets the stored revision.</summary>
	public long Revision { get; }

	/// <summary>Gets the compatible payload schema version.</summary>
	public int SchemaVersion { get; }

	/// <summary>Creates metadata for a verified positive-revision backup.</summary>
	public StoreBackup( Guid generationId, long revision, int schemaVersion )
	{
		if ( generationId == Guid.Empty || revision < 1 || schemaVersion < 1 )
			throw new ArgumentException( "A backup requires a generation ID and positive revision/schema." );
		GenerationId = generationId;
		Revision = revision;
		SchemaVersion = schemaVersion;
	}
}
