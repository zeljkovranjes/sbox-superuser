using Sandbox;

namespace SuperuserArena.Components;

/// <summary>An arena player represented by a scene component; administration never depends on this type.</summary>
public sealed class ArenaPawn : Component
{
	/// <summary>Gets the host-assigned stable player identifier.</summary>
	public string SteamId { get; private set; }

	/// <summary>Gets the pawn's current health.</summary>
	public int Health { get; private set; } = 25;

	/// <summary>Binds a game-created pawn to a stable player identifier.</summary>
	public void Bind( string steamId ) => SteamId = new SboxSuperuser.PlayerReference( steamId ).SteamId;

	/// <summary>Applies the game-specific recovery effect on the host.</summary>
	public void Restore() => Health = 100;
}
