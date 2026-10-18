using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A read outcome with a document only when a compatible valid generation was found.</summary>
public sealed class StoreReadResult
{
	/// <summary>Gets the explicit outcome; corruption must not be reported as Missing.</summary>
	public StoreReadStatus Status { get; }

	/// <summary>Gets the valid document for Found, otherwise null.</summary>
	public VersionedStoreDocument Document { get; }

	/// <summary>Creates an outcome value without accessing storage.</summary>
	public StoreReadResult( StoreReadStatus status, VersionedStoreDocument document = null )
	{
		Status = ContractGuard.EnumValue( status, nameof( status ) );
		if ( (status == StoreReadStatus.Found) != (document is not null) )
		{
			throw new ArgumentException( "Only Found must carry a document.", nameof( document ) );
		}

		Document = document;
	}
}
