namespace SboxSuperuser;

/// <summary>One public chat line from the host's in-memory recent chat.</summary>
public sealed class AdminChatRow
{
	/// <summary>Gets the sender's Steam ID.</summary>
	public string SteamId { get; init; } = "";

	/// <summary>Gets the sender's display name when they spoke.</summary>
	public string Name { get; init; } = "";

	/// <summary>Gets the message text, with control characters replaced.</summary>
	public string Message { get; init; } = "";

	/// <summary>Gets when the host received it, in UTC.</summary>
	public DateTimeOffset CreatedUtc { get; init; }
}
