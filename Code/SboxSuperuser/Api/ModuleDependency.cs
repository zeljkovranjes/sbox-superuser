using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A dependency on a module's exact contract API version, validated before installing any candidate.</summary>
public sealed class ModuleDependency
{
	/// <summary>Gets the stable dependency key.</summary>
	public string ModuleId { get; }

	/// <summary>Gets the required positive API version, independent of package release versions.</summary>
	public int RequiredApiVersion { get; }

	/// <summary>Gets whether absence is permitted; an installed dependency must still be compatible.</summary>
	public bool IsOptional { get; }

	/// <summary>Declares a dependency without loading or starting it.</summary>
	public ModuleDependency( string moduleId, int requiredApiVersion, bool isOptional = false )
	{
		ModuleId = ContractGuard.Key( moduleId, nameof( moduleId ) );
		if ( requiredApiVersion < 1 )
		{
			throw new ArgumentOutOfRangeException( nameof( requiredApiVersion ) );
		}

		RequiredApiVersion = requiredApiVersion;
		IsOptional = isOptional;
	}
}
