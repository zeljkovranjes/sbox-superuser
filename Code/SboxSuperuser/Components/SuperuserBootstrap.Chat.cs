using Sandbox;

namespace SboxSuperuser.Components;

public sealed partial class SuperuserBootstrap : IChatEvent
{
	void IChatEvent.OnChatMessage( ChatMessageEvent message ) => _session?.GetRuntime()?.Chat?.Observe( message );
}
