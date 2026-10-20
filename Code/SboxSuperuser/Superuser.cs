using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser;

/// <summary>Entry points for host administration, authenticated dispatch, storage and chat integration.</summary>
public static partial class Superuser
{
	/// <summary>Gets the Superuser library version, such as "0.1.0-rc.1".</summary>
	public static string Version => Core.Contracts.ProductInfo.Version;

	/// <summary>Gets who made the Superuser library.</summary>
	public static string Author => Core.Contracts.ProductInfo.Author;

	/// <summary>Returns local bootstrap health without exposing owner IDs or granting authority.</summary>
	public static BootstrapDiagnostics GetDiagnostics()
	{
		var scene = Game.ActiveScene;
		var diagnostics = scene.IsValid() ? scene.GetSystem<SuperuserSession>()?.GetDiagnostics() : null;
		return diagnostics ?? new BootstrapDiagnostics( BootstrapState.Disabled, null, 0, 0, null, "No active runtime scene." );
	}

	/// <summary>Resolves only a live host-authenticated engine connection; null or stale connections fail.</summary>
	public static bool TryGetPlayerIdentity( Connection connection, out ActorIdentity identity )
	{
		identity = null;
		var scene = Game.ActiveScene;
		var runtime = scene.IsValid() ? scene.GetSystem<SuperuserSession>()?.GetRuntime() : null;
		return runtime is not null && runtime.TryResolvePlayer( connection, out identity );
	}

	/// <summary>Checks explicit owner membership after revalidating the live connection on the host.</summary>
	public static bool IsOwner( Connection connection )
	{
		var scene = Game.ActiveScene;
		var runtime = scene.IsValid() ? scene.GetSystem<SuperuserSession>()?.GetRuntime() : null;
		return runtime is not null && runtime.IsOwner( connection );
	}
}
