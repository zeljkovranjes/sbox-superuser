using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	// More than a server's worth of players; anything larger is not from our host.
	private const int MaximumGags = 512;

	/// <summary>Sends every player the Steam IDs of gagged players, so their Superuser can silence them.</summary>
	internal static void PublishVoiceGags( string[] steamIds ) => ReceiveVoiceGags( steamIds );

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private static void ReceiveVoiceGags( string[] steamIds )
	{
		if ( Rpc.Caller is not { IsHost: true } || steamIds is null || steamIds.Length > MaximumGags ) return;
		Game.ActiveScene?.GetSystem<Systems.VoiceGagSystem>()?.SetGagged( steamIds );
	}
}
