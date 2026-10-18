using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Module ownership and compatibility metadata, not a loaded-module state.</summary>
public sealed class ModuleDescriptor
{
	/// <summary>Gets the stable module key.</summary>
	public string Id { get; }

	/// <summary>Gets operator-facing help.</summary>
	public string Description { get; }

	/// <summary>Gets the positive contract API version.</summary>
	public int ApiVersion { get; }

	/// <summary>Gets a snapshot of required and optional dependencies.</summary>
	public IReadOnlyList<ModuleDependency> Dependencies { get; }

	/// <summary>Gets whether ordinary staff must be prevented from disabling this security module.</summary>
	public bool IsRequiredSecurityModule { get; }

	/// <summary>Creates metadata; duplicate, cycle and compatibility checks belong to the module manager.</summary>
	public ModuleDescriptor( string id, string description, int apiVersion,
		IEnumerable<ModuleDependency> dependencies = null, bool isRequiredSecurityModule = false )
	{
		Id = ContractGuard.Key( id, nameof( id ) );
		Description = ContractGuard.Text( description, nameof( description ) );
		if ( apiVersion < 1 )
		{
			throw new ArgumentOutOfRangeException( nameof( apiVersion ) );
		}

		ApiVersion = apiVersion;
		Dependencies = ContractGuard.Copy( dependencies ?? Array.Empty<ModuleDependency>(), nameof( dependencies ) );
		IsRequiredSecurityModule = isRequiredSecurityModule;
	}
}
