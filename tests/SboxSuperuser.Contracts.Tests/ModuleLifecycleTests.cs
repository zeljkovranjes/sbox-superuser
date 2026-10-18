using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ModuleLifecycleTests
{
	[Fact]
	public async Task FailedStartupRollsBackCommandsPermissionsAndEveryListener()
	{
		using var fixture = new ModuleFixture();
		var cleanup = new List<int>();
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			registration.RegisterPermission( new PermissionDescriptor( "sample.use", "Owned permission" ) );
			TestAdminModule.Command( registration, "sample.run" );
			registration.Track( new ModuleSubscription( () => cleanup.Add( 1 ) ) );
			registration.Track( new ModuleSubscription( () => cleanup.Add( 2 ) ) );
			throw new InvalidOperationException( "Injected startup failure" );
		} };
		fixture.Manager.Install( new[] { module } );
		Assert.Equal( CommandStatus.Failed, (await fixture.Enable( "sample" )).Status );
		Assert.Null( fixture.Rig.Registry.Find( "sample.run" ) );
		Assert.DoesNotContain( "sample.use", fixture.Rig.Policy.RegisteredPermissions );
		Assert.Equal( new[] { 2, 1 }, cleanup );
		Assert.Equal( 1, module.Stops );
		Assert.Equal( ModuleState.Faulted, fixture.State( "sample" ) );
	}

	[Fact]
	public async Task PendingStartupIsInvisibleAndCannotCommitItsOwnScope()
	{
		using var fixture = new ModuleFixture();
		var ready = new TaskCompletionSource<bool>();
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			Assert.False( registration is CommandRegistrationScope );
			TestAdminModule.Command( registration, "sample.run" );
			return ready.Task;
		} };
		fixture.Manager.Install( new[] { module } );
		var enable = fixture.Enable( "sample" );
		Assert.Null( fixture.Rig.Registry.Find( "sample.run" ) );
		Assert.Equal( CommandStatus.Conflict, (await fixture.Enable( "sample" )).Status );
		ready.SetResult( true );
		Assert.Equal( CommandStatus.Success, (await enable).Status );
		Assert.NotNull( fixture.Rig.Registry.Find( "sample.run" ) );
	}

	[Fact]
	public async Task RepeatEnableDisableLeavesOneHandlerAndRetainsStoredPolicy()
	{
		using var fixture = new ModuleFixture();
		var listeners = 0;
		var calls = 0;
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			listeners++;
			registration.Track( new ModuleSubscription( () => listeners-- ) );
			TestAdminModule.Command( registration, "sample.run", ( _, _ ) =>
			{
				calls++;
				return Task.FromResult( new CommandResult( CommandStatus.Success, "Done" ) );
			} );
			return Task.CompletedTask;
		} };
		fixture.Manager.Install( new[] { module } );
		var permissionCount = fixture.Rig.Policy.RegisteredPermissions.Count;
		for ( var i = 0; i < 5; i++ )
		{
			Assert.Equal( CommandStatus.Success, (await fixture.Enable( "sample" )).Status );
			Assert.Equal( CommandStatus.Success, (await fixture.Enable( "sample" )).Status );
			Assert.Equal( 1, listeners );
			Assert.Equal( CommandStatus.Success, (await fixture.Rig.Submit( "sample.run" )).Status );
			Assert.Equal( CommandStatus.Success, (await fixture.Disable( "sample" )).Status );
			Assert.Equal( 0, listeners );
			Assert.Equal( CommandStatus.NotFound, (await fixture.Rig.Submit( "sample.run" )).Status );
		}
		Assert.Equal( 5, calls );
		Assert.Equal( 5, module.Starts );
		Assert.Equal( 5, module.Stops );
		Assert.Equal( permissionCount, fixture.Rig.Policy.RegisteredPermissions.Count );
		Assert.True( fixture.Rig.Policy.IsOwner( DispatcherRig.OwnerId ) );
	}

	[Fact]
	public async Task DependenciesStartInOrderAndCannotStopUnderActiveConsumers()
	{
		using var fixture = new ModuleFixture();
		var started = new List<string>();
		var dependency = new TestAdminModule( "base" ) { Start = ( _, _ ) => { started.Add( "base" ); return Task.CompletedTask; } };
		var consumer = new TestAdminModule( "consumer", false, new ModuleDependency( "base", 1 ) )
			{ Start = ( _, _ ) => { started.Add( "consumer" ); return Task.CompletedTask; } };
		fixture.Manager.Install( new[] { consumer, dependency } );
		Assert.Equal( CommandStatus.Success, (await fixture.Enable( "consumer" )).Status );
		Assert.Equal( new[] { "base", "consumer" }, started );
		Assert.Equal( CommandStatus.Conflict, (await fixture.Disable( "base" )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Disable( "consumer" )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Disable( "base" )).Status );
	}

	[Fact]
	public async Task OwnerOnlyManagementAndRequiredSecurityProtectionSurviveDirectGrants()
	{
		using var fixture = new ModuleFixture();
		fixture.Manager.Install( new[] { new TestAdminModule( "security", required: true ), new TestAdminModule( "sample" ) } );
		Assert.Equal( CommandStatus.Denied, (await fixture.Enable( "sample", DispatcherRig.User )).Status );
		await fixture.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "admin.security.manage", PermissionEffect.Allow ) );
		Assert.Equal( CommandStatus.Denied, (await fixture.Enable( "sample", DispatcherRig.User )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Enable( "security" )).Status );
		Assert.Equal( CommandStatus.Denied, (await fixture.Disable( "security" )).Status );
		Assert.Equal( CommandStatus.Denied, (await fixture.Disable( "superuser.core", DispatcherRig.ConsoleActor )).Status );
		Assert.Equal( CommandStatus.Denied, (await fixture.Disable( "superuser.administration" )).Status );
	}

	[Fact]
	public void InvalidInstallDoesNotPartiallyOwnCatalogAndDirectReplacementIsRefused()
	{
		using var fixture = new ModuleFixture();
		Assert.Throws<ArgumentException>( () => fixture.Manager.Install( new[] { new TestAdminModule( "valid" ), new TestAdminModule( "bad", false, new ModuleDependency( "missing", 1 ) ) } ) );
		Assert.False( fixture.Rig.Registry.HasModule( "valid" ) );
		Assert.Equal( 2, fixture.Manager.Statuses.Count );
		fixture.Manager.Install( new[] { new TestAdminModule( "valid" ) } );
		using var replacement = new CommandRegistrationScope( fixture.Rig.Registry, "valid" );
		Assert.Throws<InvalidOperationException>( replacement.Commit );
		Assert.Throws<ArgumentException>( () => fixture.Manager.Install( new[] { new TestAdminModule( "valid" ) } ) );
	}

	[Fact]
	public async Task CollisionAtCommitRollsBackWithoutReplacingExistingCommands()
	{
		using var fixture = new ModuleFixture();
		using var existing = fixture.Rig.Register( "taken", ( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Original" ) ) );
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			TestAdminModule.Command( registration, "taken" );
			TestAdminModule.Command( registration, "sample.run" );
			return Task.CompletedTask;
		} };
		fixture.Manager.Install( new[] { module } );
		Assert.Equal( CommandStatus.Failed, (await fixture.Enable( "sample" )).Status );
		Assert.Null( fixture.Rig.Registry.Find( "sample.run" ) );
		Assert.Equal( "Original", (await fixture.Rig.Submit( "taken" )).Message );
	}
}
