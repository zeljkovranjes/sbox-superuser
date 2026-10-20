namespace SboxSuperuser;

/// <summary>Bounded, permission-filtered administration views.</summary>
public enum AdminQueryKind
{
	/// <summary>Search the live player list.</summary>
	Players,
	/// <summary>Inspect an exact online or offline Steam ID.</summary>
	Player,
	/// <summary>Read recent activity visible to the caller.</summary>
	Activity,
	/// <summary>Read installed modules; only owners receive the module list and server status.</summary>
	About,
	/// <summary>List the commands the caller may run, with their arguments.</summary>
	Commands,
	/// <summary>List active bans and text mutes across all players; search filters by Steam ID.</summary>
	Restrictions,
	/// <summary>Read groups, members and permission keys (owners only).</summary>
	Permissions,
	/// <summary>Read audit records filtered by command (search), actor and outcome, 20 per page.</summary>
	ActivitySearch,
	/// <summary>Read the last 100 public chat lines kept in memory; search filters by an exact Steam ID.</summary>
	Chat,
	/// <summary>List documented modules (empty search), or read one page: search is the module ID, page its index.</summary>
	Documentation,
	/// <summary>Read the windows modules added to the admin menu, filtered to what the caller may use.</summary>
	MenuWindows
}
