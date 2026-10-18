using SboxSuperuser;

namespace SuperuserColony;

/// <summary>The colony's optional supply capability; it does not expose its player-record representation.</summary>
public interface IColonySupplies
{
	/// <summary>Replenishes a roster member's supplies by stable player identifier.</summary>
	CommandResult Replenish( PlayerReference player );
}
