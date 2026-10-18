using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>An exact permission entry; no precedence or inheritance is evaluated here.</summary>
public sealed class PermissionEntry
{
	/// <summary>Gets the exact lowercase permission key.</summary>
	public string Key { get; }

	/// <summary>Gets allow, deny or removal intent.</summary>
	public PermissionEffect Effect { get; }

	/// <summary>Creates a policy value without changing any user's permissions.</summary>
	public PermissionEntry( string key, PermissionEffect effect )
	{
		Key = ContractGuard.Key( key, nameof( key ) );
		Effect = ContractGuard.EnumValue( effect, nameof( effect ) );
	}
}
