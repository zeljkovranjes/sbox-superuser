using Sandbox;

namespace SboxSuperuser.Engine.ChatIntegration.Platform;

internal static class UnavailableChat
{
	internal static void Observe( ChatMessageEvent message )
	{
		if ( message is null || message.Suppress || message.Sender is null || !Networking.IsHost ) return;
		// A retired/duplicate runtime must not turn command text into public ordinary chat.
		message.Suppress = true;
		ChatReplies.Send( message.Sender, Guid.NewGuid(), new CommandResult( CommandStatus.Unsupported,
			"Server administration is unavailable; ask the operator to check diagnostics and restart the session if required." ) );
	}
}
