using SboxSuperuser.Engine.Commands;
using SboxSuperuser.Engine.Moderation;
using SboxSuperuser.Engine.Votes;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class VoteTests
{
	// A vote whose clock only moves when it waits; each wait first runs whatever the test queued.
	private sealed class Fixture : IDisposable
	{
		internal readonly DispatcherRig Rig = new();
		internal readonly List<VoteSnapshot> Published = new();
		internal readonly List<string> Maps = new();
		internal readonly Queue<Action> OnWait = new();
		internal int Disconnects;
		internal DateTimeOffset Now = new( 2026, 11, 11, 0, 0, 0, TimeSpan.Zero );
		internal readonly VoteSession Votes;
		private readonly AdministrationCommands _commands;

		internal Fixture()
		{
			Rig.Players.Add( new CommandPlayer( DispatcherRig.OwnerId, "Owner" ) );
			Rig.Players.Add( new CommandPlayer( DispatcherRig.UserId, "User" ) );
			var moderation = new HostModerationService( Rig.Store, () => Rig.Active, Rig.Dispatcher.CanUse, Rig.Dispatcher.CanTarget, Rig.Errors.Add, () => Now );
			Votes = new VoteSession( Published.Add, () => Rig.Players.Select( player => player.SteamId ).ToArray(), _ =>
			{
				if ( OnWait.TryDequeue( out var step ) ) step();
				Now += TimeSpan.FromSeconds( 1 );
				return Task.CompletedTask;
			}, () => Now );
			_commands = new AdministrationCommands( Rig.Policy, null, Rig.Registry, Rig.Dispatcher, () => new CommandResult( CommandStatus.Success, "Test" ),
				moderation: moderation, disconnect: (_, _, _, _) => { Disconnects++; return Task.FromResult( new CommandResult( CommandStatus.Success, "Gone" ) ); },
				votes: Votes, changeMap: map => { Maps.Add( map ); return new CommandResult( CommandStatus.Success, "Changing." ); } );
		}

		internal void Ballot( string voter, int option ) => OnWait.Enqueue( () => Votes.Cast( Published[ ^1 ].Id, voter, option ) );

		public void Dispose()
		{
			_commands.Dispose();
			Rig.Dispose();
		}
	}

	[Fact]
	public async Task VotekickPassesWithAServerMajorityAndKicksLikeTheCommand()
	{
		using var fixture = new Fixture();
		fixture.Ballot( DispatcherRig.OwnerId, 0 );
		fixture.Ballot( DispatcherRig.UserId, 0 );
		var result = await fixture.Rig.Submit( "votekick", arguments: new[] { DispatcherRig.TargetId, "Griefing" } );
		Assert.Equal( CommandStatus.Success, result.Status );
		Assert.Equal( 1, fixture.Disconnects );
		Assert.True( fixture.Published[ ^1 ].Finished );
		Assert.Equal( 2, fixture.Published[ ^1 ].Needed );
	}

	[Fact]
	public async Task OneYesOfThreeIsNotEnoughAndNothingHappens()
	{
		using var fixture = new Fixture();
		fixture.Ballot( DispatcherRig.OwnerId, 0 );
		var result = await fixture.Rig.Submit( "votekick", arguments: new[] { DispatcherRig.TargetId, "Griefing" } );
		Assert.Contains( "No option reached 2 of 3", result.Message );
		Assert.Equal( 0, fixture.Disconnects );
	}

	[Fact]
	public async Task VetoCancelsAndForcedMapVotesTakeTheMostVotes()
	{
		using var fixture = new Fixture();
		fixture.Ballot( DispatcherRig.OwnerId, 0 );
		fixture.OnWait.Enqueue( () => fixture.Votes.Veto() );
		Assert.Contains( "vetoed", (await fixture.Rig.Submit( "votemap", arguments: new[] { "facepunch.flatgrass" } )).Message );
		Assert.Empty( fixture.Maps );

		fixture.Ballot( DispatcherRig.UserId, 1 );
		fixture.OnWait.Enqueue( () => fixture.Votes.Stop() );
		var forced = await fixture.Rig.Submit( "votemap", arguments: new[] { "force", "facepunch.flatgrass", "facepunch.construct" } );
		Assert.Equal( CommandStatus.Success, forced.Status );
		Assert.Equal( "facepunch.construct", Assert.Single( fixture.Maps ) );
	}

	[Fact]
	public async Task PlayersCanStartMapVotesButNotForceThemOrKick()
	{
		using var fixture = new Fixture();
		Assert.Equal( CommandStatus.Denied, (await fixture.Rig.Submit( "votemap", DispatcherRig.User, new[] { "force", "a", "b" } )).Status );
		Assert.Equal( CommandStatus.Denied, (await fixture.Rig.Submit( "votekick", DispatcherRig.User, new[] { DispatcherRig.TargetId, "x" } )).Status );
		Assert.Equal( CommandStatus.NoChange, (await fixture.Rig.Submit( "veto" )).Status );
	}
}
