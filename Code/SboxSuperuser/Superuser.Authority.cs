using Sandbox;
using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;
using SboxSuperuser.Engine.Bootstrap;
using SboxSuperuser.Systems;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Gets local authority health on the host main thread, or null without an eligible runtime.</summary>
	public static AuthorityDiagnostics GetAuthorityDiagnostics()
	{
		var policy = FindAuthorityRuntime()?.Policy;
		return policy is null ? null : new AuthorityDiagnostics( policy.PolicyStatus, policy.OwnerStatus,
			policy.PolicyRevision, policy.OwnerRevision, policy.OwnerCount, policy.OwnerSource, policy.LastWriteStatus );
	}

	/// <summary>Applies a typed edit from an authenticated host-side console/chat adapter; only current owners may change policy.</summary>
	public static async Task<CommandResult> ApplyPolicyChangeAsync( Connection caller, PolicyChange change,
		CommandOrigin origin, CancellationToken cancellation = default )
	{
		ArgumentNullException.ThrowIfNull( change );
		if ( origin is not (CommandOrigin.Console or CommandOrigin.Chat) )
		{
			throw new ArgumentException( "Player integrations must specify console or chat origin.", nameof( origin ) );
		}

		var runtime = FindAuthorityRuntime();
		if ( runtime is null )
		{
			return new CommandResult( CommandStatus.Unsupported, "An eligible authoritative runtime is required." );
		}

		return await runtime.Policy.ApplyAsync( () => runtime.TryResolvePlayer( caller, out var actor ) ? actor : null,
			change, origin, Guid.NewGuid(), cancellation );
	}

	/// <summary>Explains the caller's or an authorized other player's stored policy; null means no access or unavailable authority.</summary>
	public static async Task<PermissionExplanation> ExplainPermissionAsync( Connection caller, string subjectSteamId, string permission )
	{
		SteamIdentity.Validate( subjectSteamId, nameof( subjectSteamId ) );
		ContractGuard.Key( permission, nameof( permission ) );
		var runtime = FindAuthorityRuntime();
		return runtime is null ? null : await runtime.Policy.ExplainForAsync(
			() => runtime.TryResolvePlayer( caller, out var actor ) ? actor : null, subjectSteamId, permission );
	}

	/// <summary>Checks punitive target protection using live actor identity and stored online/offline target authority.</summary>
	public static async Task<TargetProtectionResult> CheckPunitiveTargetAsync( Connection caller, string targetSteamId )
	{
		SteamIdentity.Validate( targetSteamId, nameof( targetSteamId ) );
		var runtime = FindAuthorityRuntime();
		if ( runtime is null )
		{
			return new TargetProtectionResult( false, 0, 0, "An eligible authoritative runtime is required." );
		}

		await runtime.Policy.Initialization;
		var result = runtime.Policy.CheckTarget( runtime.TryResolvePlayer( caller, out var actor ) ? actor : null, targetSteamId );
		return new TargetProtectionResult( result.IsAllowed, result.ActorLevel, result.TargetLevel, result.Reason );
	}

	private static BootstrapRuntime FindAuthorityRuntime()
	{
		if ( !ThreadSafe.IsMainThread )
		{
			return null;
		}

		var scene = Game.ActiveScene;
		return scene.IsValid() ? scene.GetSystem<SuperuserSession>()?.GetRuntime() : null;
	}
}
