namespace SboxSuperuser;

/// <summary>
/// The vote in progress, or the one that just finished, as every player sees it. A game that replaces Superuser's
/// vote panel draws this and sends ballots with Superuser.CastVote.
/// </summary>
public sealed class VoteSnapshot
{
	/// <summary>Gets the vote's identity; a new vote has a new one.</summary>
	public Guid Id { get; init; }

	/// <summary>Gets what is being asked, such as "Kick Bob? Spamming".</summary>
	public string Question { get; init; } = "";

	/// <summary>Gets the choices; ballots are indexes into this.</summary>
	public string[] Options { get; init; } = Array.Empty<string>();

	/// <summary>Gets each option's votes so far.</summary>
	public int[] Counts { get; init; } = Array.Empty<int>();

	/// <summary>Gets who may vote, as Steam IDs.</summary>
	public string[] Voters { get; init; } = Array.Empty<string>();

	/// <summary>Gets each voter's choice, matching <see cref="Voters"/>; -1 before they vote.</summary>
	public int[] Ballots { get; init; } = Array.Empty<int>();

	/// <summary>Gets the votes an option needs to win, or 1 when the most votes wins.</summary>
	public int Needed { get; init; }

	/// <summary>Gets when voting closes, in UTC.</summary>
	public DateTimeOffset EndsUtc { get; init; }

	/// <summary>Gets whether voting has closed.</summary>
	public bool Finished { get; init; }

	/// <summary>Gets the result in words once finished, such as "Yes won with 6 of 10."</summary>
	public string Result { get; init; } = "";
}
