namespace SboxSuperuser;

/// <summary>One player with explicit policy in a Permissions reply (owners only).</summary>
public sealed class AdminUserRow
{
	/// <summary>Gets the player's Steam ID.</summary>
	public string SteamId { get; init; } = "";

	/// <summary>Gets the groups the player belongs to.</summary>
	public string[] Groups { get; init; } = Array.Empty<string>();

	/// <summary>Gets permissions allowed directly on the player.</summary>
	public string[] Allowed { get; init; } = Array.Empty<string>();

	/// <summary>Gets permissions denied directly on the player.</summary>
	public string[] Denied { get; init; } = Array.Empty<string>();
}
