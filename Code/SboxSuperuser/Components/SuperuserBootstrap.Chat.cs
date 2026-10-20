using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser.Components;

public sealed partial class SuperuserBootstrap : IChatEvent, IAdminChatResultEvent
{
	void IChatEvent.OnChatMessage( ChatMessageEvent message )
	{
		var chat = _session?.GetRuntime()?.Chat;
		if ( chat is not null ) chat.Observe( message );
		else if ( Application.IsDedicatedServer || Game.IsEditor && AllowEditorTesting )
			Engine.ChatIntegration.Platform.UnavailableChat.Observe( message );
		Scene.GetSystem<ChatBoxSystem>()?.Observe( message );
	}

	void IAdminChatResultEvent.OnAdminChatResult( AdminChatResultEvent notification )
	{
		if ( notification.SuppressDefaultDisplay ) return;
		var lines = Engine.ChatIntegration.ChatResultText.Lines( notification.Result );
		if ( Scene.GetSystem<ChatBoxSystem>()?.ShowPrivate( lines ) == true ) notification.SuppressDefaultDisplay = true;
	}
}
