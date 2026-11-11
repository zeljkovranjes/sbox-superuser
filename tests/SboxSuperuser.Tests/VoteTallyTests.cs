using SboxSuperuser.Core.Votes;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class VoteTallyTests
{
	private static readonly string[] Players = { "1", "2", "3", "4", "5" };
	private static readonly string[] YesNo = { "Yes", "No" };

	[Fact]
	public void MajorityNeedsMoreThanHalfOfEveryoneNotJustVoters()
	{
		var tally = new VoteTally( "Kick Bob?", YesNo, Players, VoteRule.MajorityOfServer );
		Assert.Equal( 3, tally.Needed );
		tally.Cast( "1", 0 );
		tally.Cast( "2", 0 );
		Assert.Null( tally.Decide().Winner );
		Assert.False( tally.IsSettled );
		tally.Cast( "3", 0 );
		Assert.True( tally.IsSettled );
		Assert.Equal( 0, tally.Decide().Winner );
	}

	[Fact]
	public void BallotsCanChangeAndOnlyEligiblePlayersCount()
	{
		var tally = new VoteTally( "Kick Bob?", YesNo, Players, VoteRule.MajorityOfServer );
		Assert.False( tally.Cast( "6", 0 ) );
		Assert.False( tally.Cast( "1", 2 ) );
		tally.Cast( "1", 0 );
		tally.Cast( "1", 1 );
		Assert.Equal( 0, tally.Count( 0 ) );
		Assert.Equal( 1, tally.BallotOf( "1" ) );
	}

	[Fact]
	public void PlayersWhoLeaveStopCountingTowardTheMajority()
	{
		var tally = new VoteTally( "Kick Bob?", YesNo, Players, VoteRule.MajorityOfServer );
		tally.Cast( "1", 0 );
		tally.Cast( "2", 0 );
		tally.Cast( "5", 0 );
		tally.Leave( "5" );
		tally.Leave( "4" );
		tally.Leave( "3" );
		Assert.Equal( 2, tally.Needed );
		Assert.Equal( 0, tally.Decide().Winner );
	}

	[Fact]
	public void ForcedVotesTakeTheMostVotesAndTiesFail()
	{
		var tally = new VoteTally( "Next map?", new[] { "a", "b", "c" }, Players, VoteRule.MostVotes );
		tally.Cast( "1", 1 );
		Assert.Equal( 1, tally.Decide().Winner );
		tally.Cast( "2", 2 );
		Assert.Null( tally.Decide().Winner );
		Assert.Throws<ArgumentException>( () => new VoteTally( "?", new[] { "only" }, Players, VoteRule.MostVotes ) );
	}
}
