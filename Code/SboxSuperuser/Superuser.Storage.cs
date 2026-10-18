using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Selects storage for this scene before any bootstrap is enabled; trusted host setup only, never an RPC.</summary>
	public static void ConfigureStorage( Func<AdminStorageContext, IAdminStorageBackend> factory )
	{
		ArgumentNullException.ThrowIfNull( factory );
		if ( !ThreadSafe.IsMainThread || !Game.ActiveScene.IsValid() || !Networking.IsHost )
			throw new InvalidOperationException( "Configure storage on the active host scene's main thread." );
		Game.ActiveScene.GetSystem<SuperuserSession>().ConfigureStorage( factory );
	}

	/// <summary>Gets the active host store on the main thread, or null outside an eligible bootstrap runtime.</summary>
	public static IRecoverableAdminStore GetStore()
	{
		if ( !ThreadSafe.IsMainThread )
		{
			return null;
		}

		var scene = Game.ActiveScene;
		return scene.IsValid() ? scene.GetSystem<SuperuserSession>()?.GetRuntime()?.Store : null;
	}
}
