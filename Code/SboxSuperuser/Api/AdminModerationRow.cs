namespace SboxSuperuser;

/// <summary>Permission-filtered moderation evidence; kick records require the audit outcome to confirm execution.</summary>
public sealed class AdminModerationRow
{
	/// <summary>Gets the target Steam ID; filled in server-wide views such as Restrictions.</summary>
	public string SteamId { get; init; } = "";

	/// <summary>Gets the recorded action kind.</summary>
	public string Kind { get; init; }

	/// <summary>Gets the request ID for correlation with the audit log.</summary>
	public Guid RequestId { get; init; }

	/// <summary>Gets the creation time in UTC.</summary>
	public DateTimeOffset CreatedUtc { get; init; }

	/// <summary>Gets the expiry time; null means no expiry.</summary>
	public DateTimeOffset? ExpiresUtc { get; init; }

	/// <summary>Gets whether this record is the active restriction.</summary>
	public bool Active { get; init; }

	/// <summary>Gets the operator's reason.</summary>
	public string Reason { get; init; }
}
