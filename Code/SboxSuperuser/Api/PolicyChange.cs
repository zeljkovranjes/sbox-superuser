using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;
using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser;

/// <summary>A typed policy edit request; the host rechecks ownership and persistence before applying it.</summary>
public sealed class PolicyChange
{
	internal PolicyEdit Edit { get; }

	private PolicyChange( PolicyEdit edit )
	{
		Edit = edit;
	}

	/// <summary>Requests creation of a normal group with no permissions or parents.</summary>
	public static PolicyChange CreateGroup( string groupId ) => new( new PolicyEdit( PolicyEditKind.CreateGroup, Group( groupId ) ) );

	/// <summary>Requests deletion of an unreferenced normal group; player cannot be deleted.</summary>
	public static PolicyChange DeleteGroup( string groupId ) => new( new PolicyEdit( PolicyEditKind.DeleteGroup, Group( groupId ) ) );

	/// <summary>Requests an exact parent replacement, validated for references and cycles by the host.</summary>
	public static PolicyChange SetParents( string groupId, IEnumerable<string> parents )
		=> new( new PolicyEdit( PolicyEditKind.SetParents, Group( groupId ), Parents: PolicyEntries.Groups( parents ) ) );

	/// <summary>Requests a group entry change; Unset removes the entry.</summary>
	public static PolicyChange SetGroupPermission( string groupId, string permission, PermissionEffect effect )
		=> new( new PolicyEdit( PolicyEditKind.SetGroupPermission, Group( groupId ),
			ContractGuard.Key( permission, nameof( permission ) ), Effect: (int)ContractGuard.EnumValue( effect, nameof( effect ) ) ) );

	/// <summary>Requests a group target-protection level below the reserved owner level.</summary>
	public static PolicyChange SetGroupProtection( string groupId, int level )
	{
		if ( level is < 0 or > TargetProtection.MaximumGroupLevel )
		{
			throw new ArgumentOutOfRangeException( nameof( level ) );
		}

		return new PolicyChange( new PolicyEdit( PolicyEditKind.SetGroupProtection, Group( groupId ), ProtectionLevel: level ) );
	}

	/// <summary>Requests adding a normal group to an online or offline user.</summary>
	public static PolicyChange AddUserGroup( string steamId, string groupId )
		=> new( new PolicyEdit( PolicyEditKind.AddUserGroup, SteamIdentity.Validate( steamId, nameof( steamId ) ), Group( groupId ) ) );

	/// <summary>Requests removing an explicitly assigned group; implicit player membership remains.</summary>
	public static PolicyChange RemoveUserGroup( string steamId, string groupId )
		=> new( new PolicyEdit( PolicyEditKind.RemoveUserGroup, SteamIdentity.Validate( steamId, nameof( steamId ) ), Group( groupId ) ) );

	/// <summary>Requests a direct user entry change; Unset restores inheritance evaluation.</summary>
	public static PolicyChange SetUserPermission( string steamId, string permission, PermissionEffect effect )
		=> new( new PolicyEdit( PolicyEditKind.SetUserPermission, SteamIdentity.Validate( steamId, nameof( steamId ) ),
			ContractGuard.Key( permission, nameof( permission ) ), Effect: (int)ContractGuard.EnumValue( effect, nameof( effect ) ) ) );

	private static string Group( string id )
	{
		ContractGuard.Key( id, nameof( id ) );
		if ( id == "owner" )
		{
			throw new ArgumentException( "Owner membership is reserved for dedicated-local recovery.", nameof( id ) );
		}

		return id;
	}
}
