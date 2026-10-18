using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A future resolver's revisioned explanation; this type implements no authorization rules.</summary>
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

	internal PermissionExplanation( string permission, bool isAllowed, PermissionEffect directEntry,
		IEnumerable<PermissionContribution> groups, long policyRevision, bool ownerBypass )
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
	}
}
