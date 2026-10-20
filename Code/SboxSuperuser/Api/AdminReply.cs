namespace SboxSuperuser;

/// <summary>Private query/action result shared by the default and replacement interfaces.</summary>
public sealed class AdminReply
{
	/// <summary>Gets the client request ID; it is not an authentication credential.</summary>
	public Guid RequestId { get; init; }

	/// <summary>Gets the explicit outcome.</summary>
	public CommandStatus Status { get; init; }

	/// <summary>Gets a safe operator-facing explanation.</summary>
	public string Message { get; init; } = "";

	/// <summary>Gets online search results, at most 20 rows.</summary>
	public AdminPlayerRow[] Players { get; init; } = Array.Empty<AdminPlayerRow>();

	/// <summary>Gets an exact selected Steam ID, or empty for other views.</summary>
	public string SteamId { get; init; } = "";

	/// <summary>Gets whether the selected player is currently connected.</summary>
	public bool Online { get; init; }

	/// <summary>Gets the selected player's stored role labels.</summary>
	public string[] Roles { get; init; } = Array.Empty<string>();

	/// <summary>Gets actions currently permitted for this target; the host rechecks on execution.</summary>
	public AdminAction[] Actions { get; init; } = Array.Empty<AdminAction>();

	/// <summary>Gets active restrictions the caller may inspect; for a Restrictions query, across all players.</summary>
	public AdminModerationRow[] Restrictions { get; init; } = Array.Empty<AdminModerationRow>();

	/// <summary>Gets at most 20 history entries when HistoryAllowed is true.</summary>
	public AdminModerationRow[] History { get; init; } = Array.Empty<AdminModerationRow>();

	/// <summary>Gets whether moderation history permission was present.</summary>
	public bool HistoryAllowed { get; init; }

	/// <summary>Gets at most 20 recent authorized audit records.</summary>
	public AdminActivityRow[] Activity { get; init; } = Array.Empty<AdminActivityRow>();

	/// <summary>Gets installed modules when ModulesAllowed is true.</summary>
	public AdminModuleRow[] Modules { get; init; } = Array.Empty<AdminModuleRow>();

	/// <summary>Gets whether the caller may see installed modules (owners only).</summary>
	public bool ModulesAllowed { get; init; }

	/// <summary>Gets the commands the caller may run, for a Commands query.</summary>
	public AdminCommandRow[] Commands { get; init; } = Array.Empty<AdminCommandRow>();

	/// <summary>Gets permission groups, for an owner's Permissions query.</summary>
	public AdminGroupRow[] Groups { get; init; } = Array.Empty<AdminGroupRow>();

	/// <summary>Gets players with explicit policy, for an owner's Permissions query.</summary>
	public AdminUserRow[] Users { get; init; } = Array.Empty<AdminUserRow>();

	/// <summary>Gets every registered permission key, for an owner's Permissions query.</summary>
	public string[] PermissionKeys { get; init; } = Array.Empty<string>();

	/// <summary>Gets recent public chat, newest first, for a Chat query.</summary>
	public AdminChatRow[] Chat { get; init; } = Array.Empty<AdminChatRow>();

	/// <summary>Gets the title of a documentation page, for a Documentation page query.</summary>
	public string DocumentTitle { get; init; } = "";

	/// <summary>Gets the Markdown of a documentation page, for a Documentation page query.</summary>
	public string Document { get; init; } = "";

	/// <summary>Gets a command's result fields, such as row1..row8, page and more for listings.</summary>
	public Dictionary<string, string> Data { get; init; } = new();

	/// <summary>Gets whether another player/history page is available.</summary>
	public bool HasMore { get; init; }
}
