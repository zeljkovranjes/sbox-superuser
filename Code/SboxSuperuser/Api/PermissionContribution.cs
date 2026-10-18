using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>One inherited group's contribution, including an explicit deny source.</summary>
public sealed class PermissionContribution
{
	/// <summary>Gets the contributing group's stable key.</summary>
	public string GroupId { get; }

	/// <summary>Gets this group's effect for the explained permission.</summary>
	public PermissionEffect Effect { get; }

	/// <summary>Creates diagnostic contribution data without resolving policy.</summary>
	public PermissionContribution( string groupId, PermissionEffect effect )
	{
		GroupId = ContractGuard.Key( groupId, nameof( groupId ) );
		Effect = ContractGuard.EnumValue( effect, nameof( effect ) );
	}
}
