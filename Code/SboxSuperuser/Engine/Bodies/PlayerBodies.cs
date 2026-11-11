using Sandbox;

namespace SboxSuperuser.Engine.Bodies;

/// <summary>Finds the PlayerController a connected Steam ID controls in the active scene.</summary>
internal static class PlayerBodies
{
	internal static Connection Connection( string steamId ) =>
		Sandbox.Connection.All.FirstOrDefault( connection => connection.IsActive && connection.SteamId.ToString() == steamId );

	internal static PlayerController Find( string steamId )
	{
		if ( Connection( steamId ) is not { } owner || Game.ActiveScene is not { } scene ) return null;
		return scene.GetAllComponents<PlayerController>().FirstOrDefault( controller => controller.Network.OwnerId == owner.Id );
	}

	/// <summary>The resolved body, or a NotFound result explaining why there is none.</summary>
	internal static bool TryFind( string steamId, out PlayerController controller, out CommandResult missing )
	{
		controller = Find( steamId );
		missing = controller is null
			? new CommandResult( CommandStatus.NotFound, Connection( steamId ) is null ? "That player is not connected." : "That player has no body right now." )
			: null;
		return controller is not null;
	}
}
