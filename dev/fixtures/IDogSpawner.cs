using SboxSuperuser;

namespace SuperuserCompatibilityLab;

/// <summary>Optional game capability used by the sample command.</summary>
public interface IDogSpawner
{
	/// <summary>Spawns the game's dog implementation for a previously authorized target.</summary>
	CommandResult Spawn( PlayerReference target, int count );
}
