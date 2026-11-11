using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void BroadcastChatLine( string text )
	{
		if ( Rpc.Caller is not { IsHost: true } || text is null || text.Length > 1500 ) return;
		Sandbox.Platform.Chat.AddText( text );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void BroadcastCenterText( string text )
	{
		if ( Rpc.Caller is not { IsHost: true } || text is null || text.Length > 1500 ) return;
		Game.ActiveScene?.GetSystem<Systems.CenterTextSystem>()?.Show( text );
	}
}
