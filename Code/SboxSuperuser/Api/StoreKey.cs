using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Logical module/document identity; never an operating-system path.</summary>
public sealed class StoreKey
{
	/// <summary>Gets the owning module key.</summary>
	public string ModuleId { get; }

	/// <summary>Gets the logical document key.</summary>
	public string DocumentId { get; }

	/// <summary>Creates a logical key; path resolution belongs to the host-local adapter.</summary>
	public StoreKey( string moduleId, string documentId )
	{
		ModuleId = ContractGuard.Key( moduleId, nameof( moduleId ) );
		DocumentId = ContractGuard.Key( documentId, nameof( documentId ) );
	}
}
