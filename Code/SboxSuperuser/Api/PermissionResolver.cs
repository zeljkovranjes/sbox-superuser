using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser;

/// <summary>Single-threaded policy evaluator; evaluating a supplied snapshot does not authenticate or authorize a runtime caller.</summary>
public sealed class PermissionResolver
{
	internal PolicyResolver Resolver { get; }

	/// <summary>Gets the currently installed policy revision.</summary>
	public long PolicyRevision => Resolver.Policy.Revision;

	/// <summary>Gets the registration revision used to invalidate cached unknown or removed permissions.</summary>
	public long RegistryRevision => Resolver.RegistryRevision;

	/// <summary>Creates an isolated evaluator over validated policy and explicitly registered permission keys.</summary>
	public PermissionResolver( PolicySnapshot policy, IEnumerable<PermissionDescriptor> registeredPermissions )
	{
		ArgumentNullException.ThrowIfNull( policy );
		var permissions = ContractGuard.Copy( registeredPermissions, nameof( registeredPermissions ) );
		Resolver = new PolicyResolver( policy.Graph, permissions.Select( permission => permission.Key ) );
	}

	/// <summary>Explains stored user/group policy, including offline users; this method never infers owner authority.</summary>
	public PermissionExplanation Explain( string steamId, string permission )
	{
		var result = Resolver.Explain( steamId, permission );
		return new PermissionExplanation( result.Permission, result.IsAllowed, (PermissionEffect)result.DirectEntry,
			result.Contributions.Select( entry => new PermissionContribution( entry.GroupId, (PermissionEffect)entry.Effect ) ),
			result.PolicyRevision, false, result.RegistryRevision, result.IsRegistered, result.Reason );
	}

	/// <summary>Atomically replaces a policy with a newer validated revision and invalidates every cached decision.</summary>
	public void ReplacePolicy( PolicySnapshot policy )
	{
		ArgumentNullException.ThrowIfNull( policy );
		Resolver.ReplacePolicy( policy.Graph );
	}

	/// <summary>Atomically replaces registered keys and invalidates cached decisions; duplicates reject the entire replacement.</summary>
	public void ReplaceRegisteredPermissions( IEnumerable<PermissionDescriptor> permissions )
	{
		var snapshot = ContractGuard.Copy( permissions, nameof( permissions ) );
		Resolver.ReplaceRegistered( snapshot.Select( permission => permission.Key ) );
	}
}
