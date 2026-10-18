using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A versioned aggregate payload; JSON/schema validation and serialization belong to T04.</summary>
public sealed class VersionedStoreDocument
{
	/// <summary>Gets the positive payload schema version, independent of its revision.</summary>
	public int SchemaVersion { get; }

	/// <summary>Gets the positive document revision; zero is reserved for an absent document.</summary>
	public long Revision { get; }

	/// <summary>Gets host-private JSON; never broadcast this payload or expose it as caller-safe result data.</summary>
	public string PayloadJson { get; }

	/// <summary>Creates data only; nonempty payload text does not prove valid JSON or persistence.</summary>
	public VersionedStoreDocument( int schemaVersion, long revision, string payloadJson )
	{
		if ( schemaVersion < 1 || revision < 1 )
		{
			throw new ArgumentException( "Schema version and document revision must be positive." );
		}

		SchemaVersion = schemaVersion;
		Revision = revision;
		PayloadJson = ContractGuard.Text( payloadJson, nameof( payloadJson ) );
	}
}
