using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser.Components;

public sealed partial class SuperuserBootstrap : IChatEvent, IAdminChatResultEvent, IAdminResultEvent
{
	void IAdminResultEvent.OnAdminResult( AdminReply reply ) => Scene.GetSystem<ChatBoxSystem>()?.Receive( reply );

	void IChatEvent.OnChatMessage( ChatMessageEvent message )
	{
		// The host sends its own join and leave lines, so the engine's are dropped here on every machine.
		if ( IsEnginePresence( message ) )
		{
			message.Suppress = true;
			return;
		}
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
