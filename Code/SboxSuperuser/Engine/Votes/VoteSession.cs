using SboxSuperuser.Core.Votes;

namespace SboxSuperuser.Engine.Votes;

/// <summary>
/// Runs one vote at a time on the host: opens it to everyone connected, takes ballots, publishes every change and
/// decides it when time runs out, the result can no longer change, or staff stop it. Veto closes it with no winner.
/// </summary>
internal sealed class VoteSession
{
	internal static readonly TimeSpan Duration = TimeSpan.FromSeconds( 30 );
	private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds( 250 );

	private readonly Action<VoteSnapshot> _publish;
	private readonly Func<IReadOnlyList<string>> _online;
	private readonly Func<TimeSpan, Task> _wait;
	private readonly Func<DateTimeOffset> _clock;
	private VoteTally _tally;
	private Guid _id;
	private DateTimeOffset _ends;
	private bool _stopped;
	private bool _vetoed;

	internal VoteSession( Action<VoteSnapshot> publish, Func<IReadOnlyList<string>> online, Func<TimeSpan, Task> wait = null, Func<DateTimeOffset> clock = null )
	{
		_publish = publish;
		_online = online;
		_wait = wait ?? Task.Delay;
		_clock = clock ?? (() => DateTimeOffset.UtcNow);
	}

	internal bool IsRunning => _tally is not null;

	/// <summary>Holds the vote open until it is decided; null means another vote was already running.</summary>
	internal async Task<VoteOutcome> RunAsync( string question, IReadOnlyList<string> options, VoteRule rule )
	{
		if ( IsRunning ) return null;
		_tally = new VoteTally( question, options, _online(), rule );
		_id = Guid.NewGuid();
		_ends = _clock() + Duration;
		_stopped = _vetoed = false;
		Publish( false, "" );
		try
		{
			while ( !_stopped && !_vetoed && !_tally.IsSettled && _clock() < _ends )
			{
				await _wait( Tick );
				// Players who left mid-vote stop counting either way.
				foreach ( var gone in _tally.Eligible.Except( _online() ).ToArray() ) _tally.Leave( gone );
			}
			var outcome = _vetoed ? new VoteOutcome( null, "The vote was vetoed." ) : _tally.Decide();
			Publish( true, outcome.Reason );
			return outcome;
		}
		finally
		{
			_tally = null;
		}
	}

	/// <summary>Records a ballot from a connected player; false when there is no vote or they can't vote in it.</summary>
	internal bool Cast( Guid voteId, string voter, int option )
	{
		if ( _tally is null || voteId != _id || !_tally.Cast( voter, option ) ) return false;
		Publish( false, "" );
		return true;
	}

	/// <summary>Ends the vote now and decides it on the ballots so far.</summary>
	internal bool Stop() => IsRunning && (_stopped = true);

	/// <summary>Ends the vote now with no winner.</summary>
	internal bool Veto() => IsRunning && (_vetoed = true);

	private void Publish( bool finished, string result )
	{
		var tally = _tally;
		_publish?.Invoke( new VoteSnapshot
		{
			Id = _id, Question = tally.Question, Options = tally.Options.ToArray(), Finished = finished, Result = result, EndsUtc = _ends,
			Counts = Enumerable.Range( 0, tally.Options.Count ).Select( tally.Count ).ToArray(),
			Voters = tally.Eligible.ToArray(), Ballots = tally.Eligible.Select( voter => tally.BallotOf( voter ) ?? -1 ).ToArray(),
			Needed = tally.Rule == VoteRule.MajorityOfServer ? tally.Needed : 1
		} );
	}
}
