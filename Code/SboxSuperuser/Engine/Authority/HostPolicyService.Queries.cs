using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser.Engine.Authority;

internal sealed partial class HostPolicyService : Permissions.IPermissionEvaluator
{
	internal SboxSuperuser.Core.Permissions.PolicyGraph ReadPolicy( ActorIdentity actor ) => IsUsable && CanManage( actor ) ? _resolver.Policy : null;

	/// <summary>The current policy for host-internal features such as prefixes; null while authority is unusable.</summary>
	internal SboxSuperuser.Core.Permissions.PolicyGraph CurrentPolicy => IsUsable ? _resolver.Policy : null;

	public PermissionExplanation Explain( ActorIdentity actor, string permission )
	{
		var registered = _resolver.IsRegistered( permission );
		if ( !IsUsable || actor is null )
		{
			return new PermissionExplanation( permission, false, PermissionEffect.Unset, Array.Empty<PermissionContribution>(),
				PolicyRevision, false, _resolver.RegistryRevision, registered, "Authority state is unavailable; access denied." );
		}

		if ( actor.Kind == ActorKind.Player )
		{
			return ExplainSubject( actor.SubjectId, permission );
		}

		var local = actor.Kind == ActorKind.DedicatedConsole;
		return new PermissionExplanation( permission, registered && local, PermissionEffect.Unset, Array.Empty<PermissionContribution>(),
			PolicyRevision, false, _resolver.RegistryRevision, registered,
			!registered ? "Permission is not registered; access denied." : local ? "Dedicated local console authority." : "Service requires explicitly assigned capabilities." );
	}

	internal async Task<PermissionExplanation> ExplainForAsync( Func<ActorIdentity> authenticate,
		string subjectSteamId, string permission )
	{
		await Initialization;
		var actor = authenticate();
		if ( !IsUsable || actor is null
			|| actor.SubjectId != subjectSteamId && !Explain( actor, "admin.permissions.explain" ).IsAllowed )
		{
			return null;
		}

		return ExplainSubject( subjectSteamId, permission );
	}

	private PermissionExplanation ExplainSubject( string subjectSteamId, string permission )
	{
		var result = _resolver.Explain( subjectSteamId, permission );
		var owner = result.IsRegistered && IsOwner( subjectSteamId );
		return new PermissionExplanation( result.Permission, result.IsAllowed || owner, (PermissionEffect)result.DirectEntry,
			result.Contributions.Select( entry => new PermissionContribution( entry.GroupId, (PermissionEffect)entry.Effect ) ),
			result.PolicyRevision, owner, result.RegistryRevision, result.IsRegistered,
			owner ? "Explicit configured owner authority; native engine restrictions still apply." : result.Reason );
	}

	internal ProtectionDecision CheckTarget( ActorIdentity actor, string targetSteamId )
	{
		if ( !IsUsable || actor is null || actor.Kind is not (ActorKind.Player or ActorKind.DedicatedConsole) )
		{
			return new ProtectionDecision( false, 0, 0, "Authoritative player or dedicated local console is required." );
		}

		return TargetProtection.Check( _resolver.Policy, _owners, actor.SubjectId,
			actor.Kind == ActorKind.DedicatedConsole, targetSteamId );
	}

	internal void ReplaceRegisteredPermissions( IEnumerable<string> permissions ) => _resolver.ReplaceRegistered( permissions );

	internal void OnStorageChanged( StoreKey key )
	{
		if ( key.ModuleId != "security" || key.DocumentId is not ("policy" or "owners") )
		{
			return;
		}

		if ( key.DocumentId == "policy" )
		{
			PolicyStatus = StoreReadStatus.Unknown;
		}
		else
		{
			OwnerStatus = StoreReadStatus.Unknown;
		}

		// Raw store recovery must revoke cached authorization immediately, then reload behind any active policy write.
		_ = ReloadAsync( CancellationToken.None );
	}
}
