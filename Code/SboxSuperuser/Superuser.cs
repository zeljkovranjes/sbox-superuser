using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser;

/// <summary>Host-side bootstrap diagnostics and authenticated identity access; no command dispatcher yet.</summary>
public static partial class Superuser
{
	/// <summary>Returns local bootstrap health without exposing owner IDs or granting authority.</summary>
	public static BootstrapDiagnostics GetDiagnostics()
	{
		var scene = Game.ActiveScene;
		return scene.IsValid()
			? scene.GetSystem<SuperuserSession>().GetDiagnostics()
			: new BootstrapDiagnostics( BootstrapState.Disabled, null, 0, 0, null, "No active scene." );
	}

	/// <summary>Resolves only a live host-authenticated engine connection; null or stale connections fail.</summary>
	public static bool TryGetPlayerIdentity( Connection connection, out ActorIdentity identity )
	{
		identity = null;
		var scene = Game.ActiveScene;
		var runtime = scene.IsValid() ? scene.GetSystem<SuperuserSession>().GetRuntime() : null;
		return runtime is not null && runtime.TryResolvePlayer( connection, out identity );
	}

	/// <summary>Checks explicit owner membership after revalidating the live connection on the host.</summary>
	public static bool IsOwner( Connection connection )
	{
		var scene = Game.ActiveScene;
		var runtime = scene.IsValid() ? scene.GetSystem<SuperuserSession>().GetRuntime() : null;
		return runtime is not null && runtime.IsOwner( connection );
	}
}
