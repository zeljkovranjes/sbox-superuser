using SboxSuperuser;

namespace SuperuserArena;

/// <summary>The arena's optional gameplay capability, independent of the library's player identity.</summary>
public interface IPlayerRecovery
{
	/// <summary>Restores an existing arena pawn identified by its authenticated Steam ID.</summary>
	CommandResult Restore( PlayerReference player );
}
