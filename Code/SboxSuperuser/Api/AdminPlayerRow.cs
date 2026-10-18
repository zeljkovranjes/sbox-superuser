namespace SboxSuperuser;

/// <summary>One online player visible to an authorized moderation reader.</summary>
public sealed class AdminPlayerRow
{
	/// <summary>Gets the exact Steam ID used for selection and actions.</summary>
	public string SteamId { get; init; }

	/// <summary>Gets the host's sanitized display name.</summary>
	public string Name { get; init; }

	/// <summary>Gets the player's recorded warnings; zero unless the caller may read moderation history.</summary>
	public int Warnings { get; init; }
}
