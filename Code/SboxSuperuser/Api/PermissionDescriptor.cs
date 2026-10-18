using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Registered permission metadata; a descriptor does not grant the permission.</summary>
public sealed class PermissionDescriptor
{
	/// <summary>Gets the exact lowercase key; wildcards are not supported.</summary>
	public string Key { get; }

	/// <summary>Gets operator-facing help.</summary>
	public string Description { get; }

	/// <summary>Creates metadata for a permission that a module will register.</summary>
	public PermissionDescriptor( string key, string description )
	{
		Key = ContractGuard.Key( key, nameof( key ) );
		Description = ContractGuard.Text( description, nameof( description ) );
	}
}
