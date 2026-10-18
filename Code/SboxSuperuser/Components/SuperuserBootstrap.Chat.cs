using Sandbox;

namespace SboxSuperuser.Components;

public sealed partial class SuperuserBootstrap : IChatEvent
{
	void IChatEvent.OnChatMessage( ChatMessageEvent message )
	{
		var chat = _session?.GetRuntime()?.Chat;
		if ( chat is not null ) chat.Observe( message );
		else if ( Application.IsDedicatedServer || Game.IsEditor && AllowEditorTesting )
			Engine.ChatIntegration.Platform.UnavailableChat.Observe( message );
	}
}
