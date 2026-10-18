using SboxSuperuser;

namespace SuperuserArena.Engine.Administration;

internal sealed class PawnRecovery : IPlayerRecovery
{
	private readonly Func<string, bool> _restore;

	internal PawnRecovery( Func<string, bool> restore ) => _restore = restore;

	/// <summary>Maps a stable identifier through the game's own pawn lookup.</summary>
	public CommandResult Restore( PlayerReference player ) => _restore( player.SteamId )
		? new CommandResult( CommandStatus.Success, "Arena pawn restored to 100 health." )
		: new CommandResult( CommandStatus.Unsupported, "This game has no arena pawn for that player." );
}
