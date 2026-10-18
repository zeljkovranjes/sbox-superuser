using SboxSuperuser;

namespace SuperuserColony.Engine.Administration;

internal sealed class RosterSupplies : IColonySupplies
{
	private readonly Func<string, bool> _replenish;

	internal RosterSupplies( Func<string, bool> replenish ) => _replenish = replenish;

	/// <summary>Maps a stable identifier through the game's roster capability.</summary>
	public CommandResult Replenish( PlayerReference player ) => _replenish( player.SteamId )
		? new CommandResult( CommandStatus.Success, "Colony supplies replenished to 50." )
		: new CommandResult( CommandStatus.Unsupported, "This game has no colony roster entry for that player." );
}
