namespace SboxSuperuser;

/// <summary>Minimal audit evidence already filtered by the host; arguments and private reasons are omitted.</summary>
public sealed class AdminActivityRow
{
	/// <summary>Gets the originating request ID.</summary>
	public Guid RequestId { get; init; }

	/// <summary>Gets the authenticated actor subject.</summary>
	public string Actor { get; init; }

	/// <summary>Gets the command key.</summary>
	public string Command { get; init; }

	/// <summary>Gets the creation time in UTC.</summary>
	public DateTimeOffset CreatedUtc { get; init; }

	/// <summary>Gets the recorded outcome; Unknown is never success.</summary>
	public CommandStatus Outcome { get; init; }
}
