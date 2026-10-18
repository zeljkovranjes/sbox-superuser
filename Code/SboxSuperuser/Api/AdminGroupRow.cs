namespace SboxSuperuser;

/// <summary>One permission group in a Permissions reply (owners only).</summary>
public sealed class AdminGroupRow
{
	/// <summary>Gets the group ID.</summary>
	public string Id { get; init; } = "";

	/// <summary>Gets the groups this one inherits from.</summary>
	public string[] Parents { get; init; } = Array.Empty<string>();

	/// <summary>Gets the protection level (0–99) used by target protection.</summary>
	public int Protection { get; init; }

	/// <summary>Gets the permissions this group allows directly.</summary>
	public string[] Allowed { get; init; } = Array.Empty<string>();

	/// <summary>Gets the permissions this group denies directly.</summary>
	public string[] Denied { get; init; } = Array.Empty<string>();

	/// <summary>Gets the Steam IDs assigned to this group.</summary>
	public string[] Members { get; init; } = Array.Empty<string>();
}
