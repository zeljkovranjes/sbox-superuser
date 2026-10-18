using Sandbox;

namespace SuperuserColony.Systems;

/// <summary>Stores colony players as scene-owned supply records, without a player component.</summary>
public sealed class ColonyRoster : GameObjectSystem<ColonyRoster>
{
	private readonly Dictionary<string, int> _supplies = new();

	/// <summary>Creates the roster for one scene.</summary>
	public ColonyRoster( Scene scene ) : base( scene ) { }

	/// <summary>Adds a newly authenticated game player without resetting an existing record.</summary>
	public void Join( string steamId ) => _supplies.TryAdd( new SboxSuperuser.PlayerReference( steamId ).SteamId, 3 );

	/// <summary>Gets the player's supply record, or null when the game has no such player.</summary>
	public int? Supplies( string steamId ) => _supplies.TryGetValue( steamId, out var supplies ) ? supplies : null;

	/// <summary>Replaces an existing member's supplies with the game's fixed replenishment amount.</summary>
	public bool Replenish( string steamId )
	{
		if ( !_supplies.ContainsKey( steamId ) ) return false;
		_supplies[ steamId ] = 50;
		return true;
	}
}
