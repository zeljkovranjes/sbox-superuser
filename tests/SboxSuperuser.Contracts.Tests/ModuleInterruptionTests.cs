using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ModuleInterruptionTests
{
	[Fact]
	public async Task DisableCancelsQueuedWorkAndQuarantinesAnUncooperativeActiveHandler()
	{
		using var fixture = new ModuleFixture( 30 );
		var release = new TaskCompletionSource<bool>();
		var calls = 0;
		CancellationToken activeCancellation = default;
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			TestAdminModule.Command( registration, "sample.run", async ( context, _ ) =>
			{
				calls++;
				activeCancellation = context.Cancellation;
				await release.Task;
				return new CommandResult( CommandStatus.Success, "Possibly changed state" );
			} );
			return Task.CompletedTask;
		} };
		fixture.Manager.Install( new[] { module } );
		await fixture.Enable( "sample" );
		var active = fixture.Rig.Submit( "sample.run" );
		var queued = fixture.Rig.Submit( "sample.run" );
		Assert.Equal( CommandStatus.Unknown, (await fixture.Disable( "sample" )).Status );
		Assert.Equal( CommandStatus.Cancelled, (await queued).Status );
		Assert.True( activeCancellation.IsCancellationRequested );
		Assert.Null( fixture.Rig.Registry.Find( "sample.run" ) );
		Assert.Equal( ModuleState.Quarantined, fixture.State( "sample" ) );
		Assert.Equal( CommandStatus.Conflict, (await fixture.Enable( "sample" )).Status );
		Assert.Equal( 0, module.Stops );
		release.SetResult( true );
		Assert.Equal( CommandStatus.Unknown, (await active).Status );
		Assert.Equal( ModuleState.Disabled, fixture.State( "sample" ) );
		Assert.Equal( 1, module.Stops );
		Assert.Equal( 1, calls );
		Assert.Equal( CommandStatus.Success, (await fixture.Enable( "sample" )).Status );
	}

	[Fact]
	public async Task TimedOutStartupCannotPublishLateAndStopWaitsForItsCompletion()
	{
		using var fixture = new ModuleFixture( 30 );
		var release = new TaskCompletionSource<bool>();
		IModuleRegistration retained = null;
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			retained = registration;
			TestAdminModule.Command( registration, "sample.run" );
			return release.Task;
		} };
		fixture.Manager.Install( new[] { module } );
		Assert.Equal( CommandStatus.Unknown, (await fixture.Enable( "sample" )).Status );
		Assert.Equal( 0, module.Stops );
		Assert.Equal( ModuleState.Quarantined, fixture.State( "sample" ) );
		var disposed = false;
		Assert.Throws<InvalidOperationException>( () => retained.Track( new ModuleSubscription( () => disposed = true ) ) );
		Assert.True( disposed );
		Assert.Throws<InvalidOperationException>( () => TestAdminModule.Command( retained, "late" ) );
		Assert.Null( fixture.Rig.Registry.Find( "sample.run" ) );
		release.SetResult( true );
		Assert.Equal( ModuleState.Faulted, fixture.State( "sample" ) );
		Assert.Equal( 1, module.Stops );
	}

	[Fact]
	public async Task ThrowingCleanupDoesNotSkipOtherSubscriptionsAndPreventsRestart()
	{
		using var fixture = new ModuleFixture();
		var cleanup = new List<int>();
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			registration.Track( new ModuleSubscription( () => cleanup.Add( 1 ) ) );
			registration.Track( new ModuleSubscription( () => throw new InvalidOperationException( "Injected cleanup failure" ) ) );
			registration.Track( new ModuleSubscription( () => cleanup.Add( 3 ) ) );
			TestAdminModule.Command( registration, "sample.run" );
			return Task.CompletedTask;
		} };
		fixture.Manager.Install( new[] { module } );
		await fixture.Enable( "sample" );
		Assert.Equal( CommandStatus.Unknown, (await fixture.Disable( "sample" )).Status );
		Assert.Equal( new[] { 3, 1 }, cleanup );
		Assert.Equal( 1, module.Stops );
		Assert.Equal( CommandStatus.Conflict, (await fixture.Enable( "sample" )).Status );
		Assert.Null( fixture.Rig.Registry.Find( "sample.run" ) );
	}

	[Fact]
	public async Task SessionDisposalRemovesEntriesAndListenersWithoutWaitingForStartup()
	{
		using var fixture = new ModuleFixture();
		var release = new TaskCompletionSource<bool>();
		var disposed = 0;
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			registration.Track( new ModuleSubscription( () => disposed++ ) );
			return release.Task;
		} };
		fixture.Manager.Install( new[] { module } );
		var startup = fixture.Enable( "sample" );
		fixture.Manager.Dispose();
		Assert.Equal( 1, disposed );
		release.SetResult( true );
		Assert.NotEqual( CommandStatus.Success, (await startup).Status );
		Assert.Equal( CommandStatus.Unsupported, (await fixture.Enable( "sample" )).Status );
		Assert.Equal( 1, module.Stops );
	}
}
