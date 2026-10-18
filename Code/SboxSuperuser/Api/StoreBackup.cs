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

	internal StoreBackup( Guid generationId, long revision, int schemaVersion )
	{
		GenerationId = generationId;
		Revision = revision;
		SchemaVersion = schemaVersion;
	}
}
