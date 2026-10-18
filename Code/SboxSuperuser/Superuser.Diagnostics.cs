using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser;

public static partial class Superuser
{
	[ConCmd( "superuser_status" )]
	private static void PrintStatus()
	{
		var status = GetDiagnostics();
		Log.Info( $"[Superuser] state={status.State}; runtime={status.RuntimeId}; bootstraps={status.BootstrapCount}; owners={status.OwnerCount}; file={status.OwnerConfigurationPath}; {status.Message}" );
	}

	[ConCmd( "superuser_identity", ConVarFlags.Server )]
	private static void PrintPlayerIdentity( Connection caller, params string[] arguments )
	{
		if ( arguments.Length != 0 || !TryGetPlayerIdentity( caller, out var identity ) )
		{
			Log.Warning( "[Superuser] Identity refused: requires a live authenticated player and accepts no identity or role arguments." );
			return;
		}

		Log.Info( $"[Superuser] kind={identity.Kind}; subject={identity.SubjectId}; owner={IsOwner( caller )}; connection={caller.Id}" );
	}

	[ConCmd( "superuser_local_identity" )]
	private static void PrintDedicatedConsoleIdentity()
	{
		var scene = Game.ActiveScene;
		var runtime = scene.IsValid() ? scene.GetSystem<SuperuserSession>()?.GetRuntime() : null;
		if ( runtime is null || !runtime.TryResolveDedicatedConsole( out var identity ) )
		{
			Log.Warning( "[Superuser] Local console authority refused: requires the non-forwarded entry on an actual dedicated runtime." );
			return;
		}

		Log.Info( $"[Superuser] kind={identity.Kind}; subject={identity.SubjectId}; trustedLocalEntry=True; no null-caller inference" );
	}
}
