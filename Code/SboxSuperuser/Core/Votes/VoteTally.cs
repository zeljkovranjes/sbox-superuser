namespace SboxSuperuser.Core.Votes;

/// <summary>How a vote is decided.</summary>
internal enum VoteRule
{
	/// <summary>An option wins only with more than half of everyone connected, voted or not.</summary>
	MajorityOfServer,

	/// <summary>The option with the most votes wins, at least one; staff-forced votes use this.</summary>
	MostVotes
}

/// <summary>The decided outcome: the winning option's index, or null when nothing won.</summary>
internal sealed record VoteOutcome( int? Winner, string Reason );

/// <summary>
/// One vote's ballots. Only players present when it started may vote, each once, and they may change their mind
/// until it ends; anyone who leaves is no longer counted, so they neither help nor block a majority.
/// </summary>
internal sealed class VoteTally
{
	internal const int MaximumOptions = 9;

	private readonly Dictionary<string, int> _ballots = new( StringComparer.Ordinal );
	private readonly List<string> _eligible;

	internal string Question { get; }
	internal IReadOnlyList<string> Options { get; }
	internal VoteRule Rule { get; }
	internal IReadOnlyList<string> Eligible => _eligible;

	internal VoteTally( string question, IReadOnlyList<string> options, IEnumerable<string> eligible, VoteRule rule )
	{
		if ( string.IsNullOrWhiteSpace( question ) ) throw new ArgumentException( "A vote needs a question.", nameof( question ) );
		if ( options is null || options.Count < 2 || options.Count > MaximumOptions || options.Any( string.IsNullOrWhiteSpace ) )
			throw new ArgumentException( $"A vote needs 2 to {MaximumOptions} options.", nameof( options ) );
		Question = question;
		Options = options.ToArray();
		Rule = rule;
		_eligible = eligible.Distinct( StringComparer.Ordinal ).ToList();
	}

	/// <summary>Records or changes a ballot; false for players who can't vote here or options that don't exist.</summary>
	internal bool Cast( string voter, int option )
	{
		if ( option < 0 || option >= Options.Count || !_eligible.Contains( voter ) ) return false;
		_ballots[ voter ] = option;
		return true;
	}

	/// <summary>The voter's chosen option, or null before they vote.</summary>
	internal int? BallotOf( string voter ) => _ballots.TryGetValue( voter, out var option ) ? option : null;

	internal void Leave( string voter )
	{
		_eligible.Remove( voter );
		_ballots.Remove( voter );
	}

	internal int Count( int option ) => _ballots.Values.Count( ballot => ballot == option );

	/// <summary>Votes an option needs to win outright under the majority rule.</summary>
	internal int Needed => _eligible.Count / 2 + 1;

	/// <summary>Whether the result can no longer change, so the vote can end early.</summary>
	internal bool IsSettled => Rule == VoteRule.MajorityOfServer && Enumerable.Range( 0, Options.Count ).Any( option => Count( option ) >= Needed )
		|| _ballots.Count == _eligible.Count;

	internal VoteOutcome Decide()
	{
		var counts = Enumerable.Range( 0, Options.Count ).Select( Count ).ToArray();
		var best = counts.Max();
		if ( best == 0 ) return new VoteOutcome( null, "Nobody voted." );
		var leaders = Enumerable.Range( 0, counts.Length ).Where( option => counts[ option ] == best ).ToArray();
		if ( Rule == VoteRule.MajorityOfServer )
			return best >= Needed ? new VoteOutcome( leaders[ 0 ], $"{Options[ leaders[ 0 ] ]} won with {best} of {_eligible.Count}." )
				: new VoteOutcome( null, $"No option reached {Needed} of {_eligible.Count} players." );
		return leaders.Length == 1 ? new VoteOutcome( leaders[ 0 ], $"{Options[ leaders[ 0 ] ]} won with {best} votes." )
			: new VoteOutcome( null, "The vote was tied." );
	}
}
