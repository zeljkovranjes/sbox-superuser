using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser;

public static partial class Superuser
{
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
