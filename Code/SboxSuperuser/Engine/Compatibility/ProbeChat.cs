using Sandbox;
using Sandbox.Platform;

namespace SboxSuperuser.Engine.Compatibility;

internal static class ProbeChat
{
	internal static void Observe( ChatMessageEvent message )
	{
		if ( message.Suppress || message.Sender is null || message.Message != "/superuser-probe" )
		{
			return;
		}

		message.Suppress = true;
		Log.Info( $"[Superuser T01] chat suppressed; sender={message.Sender.SteamId}" );
		using ( Rpc.FilterInclude( message.Sender ) )
		{
			Reply();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private static void Reply()
	{
		if ( Application.IsDedicatedServer || Rpc.Caller is null || !Rpc.Caller.IsHost )
		{
			return;
		}

		Chat.AddText( "[Superuser T01] Private probe reply. Verify a second client sees neither this nor the request." );
		Log.Info( $"[Superuser T01] private reply received; recipient={Connection.Local.Id}" );
	}
}
