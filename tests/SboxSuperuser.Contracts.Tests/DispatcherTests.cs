using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class DispatcherTests
{
	private static CommandResult Success() => new( CommandStatus.Success, "Executed" );
	private static TaskCompletionSource<CommandResult> Barrier() => new( TaskCreationOptions.RunContinuationsAsynchronously );

	[Fact]
	public async Task ForgedContextAndNullCallerNeverReachHandler()
	{
		using var rig = new DispatcherRig();
		var calls = 0;
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( Success() ); } );
		var request = new CommandRequest( "test.run", Array.Empty<string>(), Guid.NewGuid() );
		var forged = new CommandContext( DispatcherRig.ConsoleActor, CommandOrigin.Console, request.RequestId, CancellationToken.None, CommandCapabilities.Empty );
		Assert.Equal( CommandStatus.Denied, (await rig.Dispatcher.DispatchAsync( forged, request )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "test.run", authenticate: () => null )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "test.run", actor: DispatcherRig.User )).Status );
		Assert.Equal( 0, calls );
	}

	[Fact]
	public async Task RetriesReturnSameOutcomeWithoutRepeatingMutation()
	{
		using var rig = new DispatcherRig();
		var barrier = Barrier();
		var calls = 0;
		rig.Register( "test.run", ( _, _ ) => { calls++; return barrier.Task; }, new[] { new CommandArgumentDescriptor( "text", "Text", CommandArgumentKind.String ) } );
		var id = Guid.NewGuid();
		var first = rig.Submit( "test.run", arguments: new[] { "one" }, id: id );
		var retry = rig.Submit( "test.run", arguments: new[] { "one" }, id: id );
		Assert.Equal( CommandStatus.Conflict, (await rig.Submit( "test.run", arguments: new[] { "different" }, id: id )).Status );
		barrier.SetResult( Success() );
		Assert.Same( await first, await retry );
		Assert.Equal( 1, calls );
	}

	[Fact]
	public async Task QueuedPermissionRevocationPreventsMutation()
	{
		using var rig = new DispatcherRig();
		Assert.Equal( CommandStatus.Success, (await rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "test.use", PermissionEffect.Allow ) )).Status );
		var barrier = Barrier();
		rig.Register( "test.block", ( _, _ ) => barrier.Task );
		var calls = 0;
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( Success() ); } );
		var active = rig.Submit( "test.block" );
		var queued = rig.Submit( "test.run", actor: DispatcherRig.User );
		Assert.False( queued.IsCompleted );
		await rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "test.use", PermissionEffect.Deny ) );
		barrier.SetResult( Success() );
		await active;
		Assert.Equal( CommandStatus.Denied, (await queued).Status );
		Assert.Equal( 0, calls );
	}

	[Fact]
	public async Task CallerDisconnectAndRegistrationReplacementRejectQueuedWork()
	{
		using var rig = new DispatcherRig();
		var barrier = Barrier();
		rig.Register( "test.block", ( _, _ ) => barrier.Task );
		var calls = 0;
		rig.Register( "test.first", ( _, _ ) => { calls++; return Task.FromResult( Success() ); } );
		rig.Register( "test.second", ( _, _ ) => { calls++; return Task.FromResult( Success() ); } );
		var connected = true;
		var active = rig.Submit( "test.block" );
		var disconnected = rig.Submit( "test.first", authenticate: () => connected ? DispatcherRig.Owner : null );
		var replaced = rig.Submit( "test.second" );
		connected = false;
		rig.Register( "test.second", ( _, _ ) => Task.FromResult( Success() ) );
		barrier.SetResult( Success() );
		await active;
		Assert.Equal( CommandStatus.Denied, (await disconnected).Status );
		Assert.Equal( CommandStatus.Conflict, (await replaced).Status );
		Assert.Equal( 0, calls );
	}

	[Fact]
	public async Task TargetNamesResolveBeforeQueueAndAreNeverRetargeted()
	{
		using var rig = new DispatcherRig();
		var barrier = Barrier();
		rig.Register( "test.block", ( _, _ ) => barrier.Task );
		string resolved = null;
		rig.Register( "test.target", ( _, arguments ) =>
		{
			resolved = arguments.Get<PlayerReference>( "target" ).SteamId;
			return Task.FromResult( Success() );
		}, new[] { new CommandArgumentDescriptor( "target", "Target", CommandArgumentKind.Player, targetPolicy: CommandTargetPolicy.AnyPlayer ) } );
		var active = rig.Submit( "test.block" );
		var queued = rig.Submit( "test.target", arguments: new[] { "Target" } );
		rig.Players.Clear();
		rig.Players.Add( new CommandPlayer( DispatcherRig.UserId, "Target" ) );
		barrier.SetResult( Success() );
		await active;
		Assert.Equal( CommandStatus.Success, (await queued).Status );
		Assert.Equal( DispatcherRig.TargetId, resolved );
	}

	[Fact]
	public async Task OnlineTargetsMustStillBeOnlineWhenWorkExecutes()
	{
		using var rig = new DispatcherRig();
		var barrier = Barrier();
		rig.Register( "test.block", ( _, _ ) => barrier.Task );
		var calls = 0;
		rig.Register( "test.target", ( _, _ ) => { calls++; return Task.FromResult( Success() ); },
			new[] { new CommandArgumentDescriptor( "target", "Target", CommandArgumentKind.Player, targetPolicy: CommandTargetPolicy.OnlinePlayer ) } );
		var active = rig.Submit( "test.block" );
		var queued = rig.Submit( "test.target", arguments: new[] { DispatcherRig.TargetId } );
		rig.Players.Clear();
		barrier.SetResult( Success() );
		await active;
		Assert.Equal( CommandStatus.NotFound, (await queued).Status );
		Assert.Equal( 0, calls );
	}

	[Fact]
	public async Task OfflineEqualRanksCannotPunishAndPromotionWhileQueuedIsRechecked()
	{
		using var rig = new DispatcherRig();
		await rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.UserId, "moderator" ) );
		await rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "test.use", PermissionEffect.Allow ) );
		var barrier = Barrier();
		rig.Register( "test.block", ( _, _ ) => barrier.Task );
		var calls = 0;
		rig.Register( "test.punish", ( _, _ ) => { calls++; return Task.FromResult( Success() ); },
			new[] { new CommandArgumentDescriptor( "target", "Target", CommandArgumentKind.Player, targetPolicy: CommandTargetPolicy.PunitiveAnyPlayer ) } );
		var active = rig.Submit( "test.block" );
		var queued = rig.Submit( "test.punish", actor: DispatcherRig.User, arguments: new[] { DispatcherRig.TargetId } );
		await rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.TargetId, "moderator" ) );
		barrier.SetResult( Success() );
		await active;
		Assert.Equal( CommandStatus.Denied, (await queued).Status );
		Assert.Equal( 0, calls );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "test.punish", arguments: new[] { DispatcherRig.OwnerId } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Submit( "test.punish", actor: DispatcherRig.ConsoleActor, arguments: new[] { DispatcherRig.OwnerId } )).Status );
	}

	[Theory]
	[InlineData( CommandExecutionPolicy.Mutation, CommandStatus.Unknown )]
	[InlineData( CommandExecutionPolicy.ReadOnly, CommandStatus.Failed )]
	public async Task ExceptionsAreReportedPrivatelyAndNeverBecomeFalseSuccess( CommandExecutionPolicy execution, CommandStatus expected )
	{
		using var rig = new DispatcherRig();
		rig.Register( "test.fail", ( _, _ ) => throw new InvalidOperationException( "secret value must not escape" ), execution: execution );
		var result = await rig.Submit( "test.fail" );
		Assert.Equal( expected, result.Status );
		Assert.DoesNotContain( "secret", result.Message );
		Assert.Single( rig.Errors );
	}

	[Fact]
	public async Task CancellationDistinguishesQueuedFromAlreadyStartedMutation()
	{
		using var rig = new DispatcherRig();
		var barrier = Barrier();
		rig.Register( "test.block", ( _, _ ) => barrier.Task );
		var calls = 0;
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( Success() ); } );
		using var activeCancellation = new CancellationTokenSource();
		using var queuedCancellation = new CancellationTokenSource();
		var active = rig.Submit( "test.block", cancellation: activeCancellation.Token );
		var queued = rig.Submit( "test.run", cancellation: queuedCancellation.Token );
		queuedCancellation.Cancel();
		Assert.Equal( CommandStatus.Cancelled, (await queued.WaitAsync( TimeSpan.FromSeconds( 2 ) )).Status );
		activeCancellation.Cancel();
		barrier.SetResult( Success() );
		Assert.Equal( CommandStatus.Unknown, (await active).Status );
		Assert.Equal( 0, calls );
	}

	[Fact]
	public async Task CapabilitiesAreExactAndScopedToTheirOwningModule()
	{
		using var rig = new DispatcherRig();
		var capability = new object();
		rig.Register( "test.capability", ( context, _ ) => Task.FromResult( new CommandResult(
			ReferenceEquals( context.Services.GetService<object>(), capability ) ? CommandStatus.Success : CommandStatus.Unsupported, "Capability checked" ) ),
			services: scope => scope.RegisterService<object>( capability ) );
		rig.Register( "test.absent", ( context, _ ) => Task.FromResult( new CommandResult(
			context.Services.GetService<object>() is null ? CommandStatus.Unsupported : CommandStatus.Success, "Capability checked" ) ) );
		Assert.Equal( CommandStatus.Success, (await rig.Submit( "test.capability" )).Status );
		Assert.Equal( CommandStatus.Unsupported, (await rig.Submit( "test.absent" )).Status );
	}

	[Fact]
	public async Task InternalServiceHasOnlyExplicitPermissionsAndRevocationStopsQueuedWork()
	{
		using var rig = new DispatcherRig();
		var barrier = Barrier();
		rig.Register( "test.block", ( _, _ ) => barrier.Task );
		var calls = 0;
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( Success() ); } );
		CommandService Service( string[] permissions ) => new( "test.service", permissions, ( service, request, cancellation ) => rig.Dispatcher.SubmitAsync(
			() => service.IsActive ? new ActorIdentity( ActorKind.InternalService, service.Id ) : null, CommandOrigin.InternalService, request, cancellation, service ) );
		using var denied = Service( Array.Empty<string>() );
		Assert.Equal( CommandStatus.Denied, (await denied.ExecuteAsync( new CommandRequest( "test.run", Array.Empty<string>(), Guid.NewGuid() ) )).Status );
		using var allowed = Service( new[] { "test.use" } );
		Assert.Equal( CommandStatus.Success, (await allowed.ExecuteAsync( new CommandRequest( "test.run", Array.Empty<string>(), Guid.NewGuid() ) )).Status );
		var active = rig.Submit( "test.block" );
		var queued = allowed.ExecuteAsync( new CommandRequest( "test.run", Array.Empty<string>(), Guid.NewGuid() ) );
		allowed.Dispose();
		barrier.SetResult( Success() );
		await active;
		Assert.Equal( CommandStatus.Denied, (await queued).Status );
		Assert.Equal( 1, calls );
	}
}
