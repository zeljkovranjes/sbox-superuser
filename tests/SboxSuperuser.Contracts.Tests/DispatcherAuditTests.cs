using SboxSuperuser.Core.Audit;
using SboxSuperuser.Engine.Audit;
using SboxSuperuser.Engine.Authority;
using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class DispatcherAuditTests
{
	private static readonly CancellationToken None = CancellationToken.None;
	private static Task<AuditPage> Query( HostAuditService audit ) => audit.QueryAsync( () => DispatcherRig.Owner,
		_ => true, _ => false, new AuditQuery(), None );
	private static Task<CommandResult> Submit( HostCommandDispatcher dispatcher, string name = "test.run", string[] arguments = null,
		Guid? id = null, Func<ActorIdentity> actor = null, CommandOrigin origin = CommandOrigin.Console, CancellationToken cancellation = default )
		=> dispatcher.SubmitAsync( actor ?? (() => DispatcherRig.Owner), origin,
			new CommandRequest( name, arguments ?? Array.Empty<string>(), id ?? Guid.NewGuid() ), cancellation );
	private static HostCommandDispatcher Dispatcher( DispatcherRig rig, HostAuditService audit ) => new( rig.Policy, rig.Registry,
		() => rig.Active, () => rig.Players, rig.Errors.Add, audit: audit );

	[Fact]
	public async Task HandlerSeesDurableIntentAndRetryProducesOneOutcome()
	{
		using var rig = new DispatcherRig();
		var store = new AuditStoreHarness();
		var audit = new HostAuditService( store, () => true, rig.Errors.Add );
		using var dispatcher = Dispatcher( rig, audit );
		var calls = 0;
		rig.Register( "test.run", async ( _, _ ) =>
		{
			var stored = await store.ReadAsync( HostAuditService.Key, 1, None );
			Assert.Equal( "Unknown", Assert.Single( AuditFormat.ReadPayload( stored.Document.PayloadJson ).Records ).Outcome );
			calls++;
			return new CommandResult( CommandStatus.Success, "secret-result-not-for-audit" );
		} );
		var id = Guid.NewGuid();
		Assert.Equal( CommandStatus.Success, (await Submit( dispatcher, id: id )).Status );
		Assert.Equal( CommandStatus.Success, (await Submit( dispatcher, id: id )).Status );
		Assert.Equal( 1, calls );
		var entry = Assert.Single( (await Query( audit )).Entries );
		Assert.Equal( id, entry.RequestId );
		Assert.Equal( CommandStatus.Success, entry.Outcome );
		Assert.DoesNotContain( "secret-result-not-for-audit", string.Join( "", store.Files.Files.Values ) );
	}

	[Theory]
	[InlineData( false, CommandStatus.StorageUnavailable, 0 )]
	[InlineData( true, CommandStatus.Unknown, 1 )]
	public async Task StorageFailureBeforeOrAfterActionCannotReportSuccess( bool afterAction, CommandStatus expected, int expectedCalls )
	{
		using var rig = new DispatcherRig();
		var store = new AuditStoreHarness();
		var audit = new HostAuditService( store, () => true, rig.Errors.Add );
		using var dispatcher = Dispatcher( rig, audit );
		var calls = 0;
		store.BeforeWrite = _ => Task.FromResult<StoreWriteStatus?>( !afterAction || calls > 0 ? StoreWriteStatus.Unavailable : null );
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( new CommandResult( CommandStatus.Success, "done" ) ); } );
		Assert.Equal( expected, (await Submit( dispatcher )).Status );
		Assert.Equal( expectedCalls, calls );
		if ( afterAction )
		{
			Assert.Equal( CommandStatus.Unknown, Assert.Single( (await Query( audit )).Entries ).Outcome );
		}
	}

	[Fact]
	public async Task IdentityIsRecheckedAfterAwaitedIntentCommit()
	{
		using var rig = new DispatcherRig();
		var store = new AuditStoreHarness();
		var audit = new HostAuditService( store, () => true, rig.Errors.Add );
		using var dispatcher = Dispatcher( rig, audit );
		var connected = true;
		var calls = 0;
		store.BeforeWrite = _ => { connected = false; return Task.FromResult<StoreWriteStatus?>( null ); };
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( new CommandResult( CommandStatus.Success, "done" ) ); } );
		Assert.Equal( CommandStatus.Denied, (await Submit( dispatcher, actor: () => connected ? DispatcherRig.Owner : null )).Status );
		Assert.Equal( 0, calls );
		Assert.Equal( CommandStatus.Denied, Assert.Single( (await Query( audit )).Entries ).Outcome );
	}

	[Fact]
	public async Task DefaultRedactionAndSensitiveChatRefusalNeverPersistSecrets()
	{
		using var rig = new DispatcherRig();
		var store = new AuditStoreHarness();
		var audit = new HostAuditService( store, () => true, rig.Errors.Add );
		using var dispatcher = Dispatcher( rig, audit );
		var calls = 0;
		rig.Register( "test.run", ( _, _ ) => { calls++; return Task.FromResult( new CommandResult( CommandStatus.Success, "done" ) ); }, new[]
		{
			new CommandArgumentDescriptor( "password", "Password", CommandArgumentKind.String, audit: CommandArgumentAudit.Sensitive ),
			new CommandArgumentDescriptor( "count", "Count", CommandArgumentKind.Integer, audit: CommandArgumentAudit.SafeValue )
		} );
		var arguments = new[] { "unique-secret-sentinel", "42" };
		Assert.Equal( CommandStatus.Denied, (await Submit( dispatcher, arguments: arguments, origin: CommandOrigin.Chat )).Status );
		Assert.Equal( CommandStatus.Success, (await Submit( dispatcher, arguments: arguments )).Status );
		Assert.Equal( 1, calls );
		Assert.DoesNotContain( arguments[ 0 ], string.Join( "", store.Files.Files.Values ) );
		var success = (await Query( audit )).Entries.Single( entry => entry.Outcome == CommandStatus.Success );
		Assert.Equal( "[redacted]", success.SafeArguments[ "password" ] );
		Assert.Equal( "42", success.SafeArguments[ "count" ] );
	}

	[Fact]
	public async Task DenialParsingFailureAndHandlerFailureHaveDistinctOutcomes()
	{
		using var rig = new DispatcherRig();
		var audit = new HostAuditService( new AuditStoreHarness(), () => true, rig.Errors.Add );
		using var dispatcher = Dispatcher( rig, audit );
		rig.Register( "test.run", ( _, _ ) => throw new IOException( "private" ), new[]
		{
			new CommandArgumentDescriptor( "count", "Count", CommandArgumentKind.Integer )
		}, execution: CommandExecutionPolicy.ReadOnly );
		await Submit( dispatcher, actor: () => DispatcherRig.User );
		await Submit( dispatcher, arguments: new[] { "invalid" } );
		await Submit( dispatcher, arguments: new[] { "1" } );
		Assert.Equal( new[] { CommandStatus.Failed, CommandStatus.InvalidArguments, CommandStatus.Denied },
			(await Query( audit )).Entries.Select( entry => entry.Outcome ) );
	}

	[Fact]
	public async Task QueuedCancellationIsRecordedWithoutInvokingHandler()
	{
		using var rig = new DispatcherRig();
		var audit = new HostAuditService( new AuditStoreHarness(), () => true, rig.Errors.Add );
		using var dispatcher = Dispatcher( rig, audit );
		var held = new TaskCompletionSource<CommandResult>( TaskCreationOptions.RunContinuationsAsynchronously );
		rig.Register( "test.run", ( _, _ ) => held.Task );
		var first = Submit( dispatcher );
		using var cancellation = new CancellationTokenSource();
		var second = Submit( dispatcher, cancellation: cancellation.Token );
		cancellation.Cancel();
		Assert.Equal( CommandStatus.Cancelled, (await second).Status );
		held.SetResult( new CommandResult( CommandStatus.Success, "done" ) );
		await first;
		Assert.Equal( new[] { CommandStatus.Cancelled, CommandStatus.Success }, (await Query( audit )).Entries.Select( entry => entry.Outcome ) );
	}

	[Fact]
	public async Task DirectPolicyChangeRequiresIntentAndCorrelatesAggregateWithOutcome()
	{
		var store = new AuditStoreHarness();
		var audit = new HostAuditService( store, () => true, _ => { } );
		var policy = new HostPolicyService( store, new[] { DispatcherRig.OwnerId }, StoreReadStatus.Found, () => true, _ => { }, audit );
		var id = Guid.NewGuid();
		Assert.Equal( CommandStatus.Success, (await policy.ApplyAsync( () => DispatcherRig.Owner,
			PolicyChange.AddUserGroup( DispatcherRig.UserId, "moderator" ), CommandOrigin.Console, id, None )).Status );
		Assert.Equal( id, Assert.Single( (await Query( audit )).Entries ).RequestId );
		Assert.Contains( id.ToString(), (await store.ReadAsync( HostPolicyService.PolicyKey, 1, None )).Document.PayloadJson );
		store.BeforeWrite = _ => Task.FromResult<StoreWriteStatus?>( StoreWriteStatus.Unavailable );
		Assert.Equal( CommandStatus.StorageUnavailable, (await policy.ApplyAsync( () => DispatcherRig.Owner,
			PolicyChange.AddUserGroup( DispatcherRig.UserId, "admin" ), CommandOrigin.Console, Guid.NewGuid(), None )).Status );
		Assert.Equal( 1, policy.PolicyRevision );
	}
}
