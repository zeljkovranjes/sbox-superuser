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

	/// <summary>Gets active restrictions the caller may inspect.</summary>
	public AdminModerationRow[] Restrictions { get; init; } = Array.Empty<AdminModerationRow>();

	/// <summary>Gets at most 20 history entries when HistoryAllowed is true.</summary>
	public AdminModerationRow[] History { get; init; } = Array.Empty<AdminModerationRow>();

	/// <summary>Gets whether moderation history permission was present.</summary>
	public bool HistoryAllowed { get; init; }

	/// <summary>Gets at most 20 recent authorized audit records.</summary>
	public AdminActivityRow[] Activity { get; init; } = Array.Empty<AdminActivityRow>();

	/// <summary>Gets whether another player/history page is available.</summary>
	public bool HasMore { get; init; }
}
