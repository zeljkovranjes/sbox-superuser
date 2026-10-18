using Sandbox;
using Sandbox.Network;

namespace SuperuserCompatibilityLab.Components;

/// <summary>Starts the isolated network session used to verify the Superuser library.</summary>
public sealed class LabSession : Component
{
	protected override void OnStart()
	{
		if ( (!Application.IsDedicatedServer && !Game.IsEditor) || Networking.IsActive )
		{
			return;
		}

		Networking.CreateLobby( new LobbyConfig
		{
			Name = "Superuser T01 Local Validation",
			Privacy = LobbyPrivacy.Private,
			Hidden = true,
			MaxPlayers = 4
		} );
	}
}
