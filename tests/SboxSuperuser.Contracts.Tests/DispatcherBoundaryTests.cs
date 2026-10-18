using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class DispatcherBoundaryTests
{
	[Fact]
	public async Task ForeignDispatcherAndMismatchedRequestIdsCannotReuseIssuedContext()
	{
		using var first = new DispatcherRig();
		using var second = new DispatcherRig();
		var request = new CommandRequest( "test.run", Array.Empty<string>(), Guid.NewGuid() );
		var services = new CommandExecutionServices( first.Dispatcher, () => DispatcherRig.Owner, null );
		var context = new CommandContext( DispatcherRig.Owner, CommandOrigin.Console, request.RequestId, CancellationToken.None, services );
		Assert.Equal( CommandStatus.Denied, (await second.Dispatcher.DispatchAsync( context, request )).Status );
		Assert.Equal( CommandStatus.Denied, (await first.Dispatcher.DispatchAsync( context,
			new CommandRequest( "test.run", Array.Empty<string>(), Guid.NewGuid() ) )).Status );
	}

	[Theory]
	[InlineData( ActorKind.Player, CommandOrigin.InternalService )]
	[InlineData( ActorKind.DedicatedConsole, CommandOrigin.Chat )]
	[InlineData( ActorKind.InternalService, CommandOrigin.Console )]
	public async Task ActorKindsCannotImpersonateAnotherIngress( ActorKind kind, CommandOrigin origin )
	{
		using var rig = new DispatcherRig();
		var actor = kind == ActorKind.Player ? DispatcherRig.Owner : kind == ActorKind.DedicatedConsole
			? DispatcherRig.ConsoleActor : new ActorIdentity( ActorKind.InternalService, "test.service" );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "test.run", actor: actor, origin: origin )).Status );
	}

	[Fact]
	public async Task InvalidInputAndOversizedResultsNeverReportSuccess()
	{
		using var rig = new DispatcherRig();
		var calls = 0;
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( new CommandResult( CommandStatus.Success, new string( 'x', 1025 ) ) ); } );
		Assert.Equal( CommandStatus.InvalidArguments, (await rig.Submit( "test.run", arguments: new[] { new string( 'x', 513 ) } )).Status );
		Assert.Equal( 0, calls );
		Assert.Equal( CommandStatus.Unknown, (await rig.Submit( "test.run" )).Status );
		Assert.Equal( 1, calls );
	}

	[Fact]
	public async Task DegradedAuthorityAndRetiredRuntimeFailClosed()
	{
		using var rig = new DispatcherRig();
		rig.Register( "test.run", ( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Must not execute" ) ) );
		rig.Store.ReadOverrides[ "security/policy" ] = StoreReadStatus.Corrupt;
		await rig.Policy.ReloadAsync( CancellationToken.None );
		Assert.Equal( CommandStatus.StorageUnavailable, (await rig.Submit( "test.run" )).Status );
		rig.Active = false;
		Assert.Equal( CommandStatus.Unsupported, (await rig.Submit( "test.run" )).Status );
	}

	[Fact]
	public async Task CompletedRetryReturnsOwnOutcomeAfterTargetDirectoryChanges()
	{
		using var rig = new DispatcherRig();
		var calls = 0;
		rig.Register( "test.target", ( _, _ ) => { calls++; return Task.FromResult( new CommandResult( CommandStatus.Success, "Executed" ) ); },
			new[] { new CommandArgumentDescriptor( "target", "Target", CommandArgumentKind.Player, targetPolicy: CommandTargetPolicy.OnlinePlayer ) } );
		var id = Guid.NewGuid();
		var first = await rig.Submit( "test.target", arguments: new[] { "Target" }, id: id );
		rig.Players.Clear();
		Assert.Same( first, await rig.Submit( "test.target", arguments: new[] { "Target" }, id: id ) );
		Assert.Equal( 1, calls );
	}
}
