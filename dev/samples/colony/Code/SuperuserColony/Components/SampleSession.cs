using Sandbox;
using Sandbox.Network;

namespace SuperuserColony.Components;

/// <summary>Starts an isolated engine network session for this development sample.</summary>
public sealed class SampleSession : Component
{
	protected override void OnStart()
	{
		if ( Networking.IsActive || !Application.IsDedicatedServer && !Game.IsEditor ) return;
		Networking.CreateLobby( new LobbyConfig
		{
			Name = "Superuser colony sample",
			Privacy = LobbyPrivacy.Private,
			Hidden = true,
			MaxPlayers = 4
		} );
	}
}
