namespace SboxSuperuser;

/// <summary>
/// What Superuser's player commands do to a player's body, run on the host after permission and target checks.
/// Players are exact Steam IDs. Return Unsupported for anything the game has no concept of, NotFound when the
/// player has no body, and Success otherwise. PlayerControllerActions is the default for games that use the
/// engine's PlayerController; derive from it to supply what only the game knows, such as health and armor.
/// </summary>
public interface IPlayerActions
{
	/// <summary>Kills the player; silent kills skip effects a game would show others.</summary>
	CommandResult Kill( string steamId, bool silent );

	/// <summary>Knocks the player in a random direction and deals the damage, if any.</summary>
	CommandResult Slap( string steamId, float damage );

	/// <summary>Stops or restores the player's movement and input.</summary>
	CommandResult SetFrozen( string steamId, bool frozen );

	/// <summary>Blacks out or restores the player's screen.</summary>
	CommandResult SetBlind( string steamId, bool blind );

	/// <summary>Sets the player on fire or puts them out.</summary>
	CommandResult SetIgnited( string steamId, bool ignited );

	/// <summary>Hides or shows the player's body to everyone.</summary>
	CommandResult SetCloaked( string steamId, bool cloaked );

	/// <summary>Makes the player immune to damage, or not.</summary>
	CommandResult SetGod( string steamId, bool god );

	/// <summary>Sets the player's health.</summary>
	CommandResult SetHealth( string steamId, int health );

	/// <summary>Sets the player's armor.</summary>
	CommandResult SetArmor( string steamId, int armor );

	/// <summary>Takes away the player's weapons and items.</summary>
	CommandResult Strip( string steamId );

	/// <summary>Turns the player into a ragdoll, or stands them back up where it lies.</summary>
	CommandResult SetRagdoll( string steamId, bool ragdoll );

	/// <summary>Lets the player fly through the world, or returns them to walking.</summary>
	CommandResult SetNoclip( string steamId, bool noclip );

	/// <summary>Points the spectator's camera at the target, or gives it back when target is null.</summary>
	CommandResult Spectate( string spectatorSteamId, string targetSteamId );

	/// <summary>Moves the target next to the destination player, facing them.</summary>
	CommandResult MoveTo( string steamId, string destinationSteamId );

	/// <summary>Moves the target to where the aiming player is looking.</summary>
	CommandResult MoveToAim( string steamId, string aimingSteamId );

	/// <summary>Puts the target back where they were before their last move.</summary>
	CommandResult Return( string steamId );

	/// <summary>Builds a cage around the player, or removes it.</summary>
	CommandResult SetJailed( string steamId, bool jailed );

	/// <summary>Plays a sound asset for everyone.</summary>
	CommandResult PlaySound( string sound );

	/// <summary>Changes the map for everyone, keeping them connected.</summary>
	CommandResult ChangeMap( string map );
}
