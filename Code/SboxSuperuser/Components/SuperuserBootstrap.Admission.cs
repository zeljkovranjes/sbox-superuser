using Sandbox;

namespace SboxSuperuser.Components;

public sealed partial class SuperuserBootstrap : Component.INetworkListener
{
	bool Component.INetworkListener.AcceptConnection( Connection connection, ref string reason )
	{
		if ( !Networking.IsHost || !Application.IsDedicatedServer && !(Game.IsEditor && AllowEditorTesting) ) return true;
		var runtime = _session?.GetRuntime();
		if ( runtime is not null ) return runtime.AcceptConnection( connection, ref reason );
		reason = "Server moderation is not ready; try again later.";
		return false;
	}
}
