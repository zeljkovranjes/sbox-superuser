using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A revisioned permission explanation with direct entries, inherited sources and an explicit diagnostic.</summary>
public sealed class PermissionExplanation
{
	/// <summary>Gets the permission being explained.</summary>
	public string Permission { get; }

	/// <summary>Gets the explicit final decision; false is deny.</summary>
	public bool IsAllowed { get; }

	/// <summary>Gets the direct user entry, or Unset when absent.</summary>
	public PermissionEffect DirectEntry { get; }

	/// <summary>Gets contributing inherited groups, including deny sources.</summary>
	public IReadOnlyList<PermissionContribution> Groups { get; }

	/// <summary>Gets the policy revision at which this decision was made.</summary>
	public long PolicyRevision { get; }

	/// <summary>Gets whether explicit owner authority determined this framework decision.</summary>
	public bool OwnerBypass { get; }

	/// <summary>Gets the permission registry revision used for this decision.</summary>
	public long RegistryRevision { get; }

	/// <summary>Gets whether this exact permission was registered; unknown keys always deny.</summary>
	public bool IsRegistered { get; }

	/// <summary>Gets a safe explanation of the winning rule or unknown-key denial.</summary>
	public string Reason { get; }

	internal PermissionExplanation( string permission, bool isAllowed, PermissionEffect directEntry,
		IEnumerable<PermissionContribution> groups, long policyRevision, bool ownerBypass,
		long registryRevision = 0, bool isRegistered = false, string reason = "No resolver diagnostic supplied." )
	{
		Permission = ContractGuard.Key( permission, nameof( permission ) );
		if ( policyRevision < 0 )
		{
			throw new ArgumentOutOfRangeException( nameof( policyRevision ) );
		}

		IsAllowed = isAllowed;
		DirectEntry = ContractGuard.EnumValue( directEntry, nameof( directEntry ) );
		Groups = ContractGuard.Copy( groups, nameof( groups ) );
		PolicyRevision = policyRevision;
		OwnerBypass = ownerBypass;
		RegistryRevision = registryRevision;
		IsRegistered = isRegistered;
		Reason = reason;
	}
}
