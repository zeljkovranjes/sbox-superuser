using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser;

/// <summary>Immutable revisioned user/group policy with validated references and acyclic inheritance.</summary>
public sealed class PolicySnapshot
{
	internal PolicyGraph Graph { get; }

	/// <summary>Gets the policy revision; replacement requires a strictly newer revision.</summary>
	public long Revision => Graph.Revision;

	/// <summary>Gets all immutable named groups, including the required player group.</summary>
	public IReadOnlyList<GroupPolicy> Groups { get; }

	/// <summary>Gets explicitly stored users; other Steam IDs still inherit player.</summary>
	public IReadOnlyList<UserPolicy> Users { get; }

	/// <summary>Validates the complete graph before making any policy available to a resolver.</summary>
	public PolicySnapshot( long revision, IEnumerable<GroupPolicy> groups, IEnumerable<UserPolicy> users = null )
	{
		Groups = ContractGuard.Copy( groups, nameof( groups ) );
		Users = ContractGuard.Copy( users ?? Array.Empty<UserPolicy>(), nameof( users ) );
		Graph = new PolicyGraph( revision, Groups.Select( group => group.Definition ), Users.Select( user => user.Definition ) );
	}
}
