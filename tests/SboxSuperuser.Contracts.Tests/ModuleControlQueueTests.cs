using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ModuleControlQueueTests
{
	[Fact]
	public async Task AuditedControlPathCanDisableAModuleWhileNormalQueueIsOccupied()
	{
		using var fixture = new ModuleFixture( 30 );
		using var administration = new AdministrationCommands( fixture.Rig.Policy, null, fixture.Rig.Registry,
			fixture.Rig.Dispatcher, () => new CommandResult( CommandStatus.Success, "Test health" ), fixture.Manager );
		var release = new TaskCompletionSource<bool>();
		var calls = 0;
		var module = new TestAdminModule( "sample" ) { Start = ( registration, _ ) =>
		{
			TestAdminModule.Command( registration, "sample.run", async ( _, _ ) =>
			{
				calls++;
				await release.Task;
				return new CommandResult( CommandStatus.Success, "Done" );
			} );
			return Task.CompletedTask;
		} };
		fixture.Manager.Install( new[] { module } );
		await fixture.Enable( "sample" );
		var activeId = Guid.NewGuid();
		var active = fixture.Rig.Submit( "sample.run", id: activeId );
		Assert.Equal( CommandStatus.Conflict, (await fixture.Rig.Submit( "modules.disable", arguments: new[] { "sample" }, id: activeId )).Status );
		await fixture.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "admin.security.manage", PermissionEffect.Allow ) );
		Assert.Equal( CommandStatus.Denied, (await fixture.Rig.Submit( "modules.disable", DispatcherRig.User, new[] { "sample" } )).Status );
		var controlId = Guid.NewGuid();
		var disable = await fixture.Rig.Submit( "modules.disable", arguments: new[] { "sample" }, id: controlId ).WaitAsync( TimeSpan.FromSeconds( 2 ) );
		Assert.Equal( CommandStatus.Unknown, disable.Status );
		Assert.Equal( ModuleState.Quarantined, fixture.State( "sample" ) );
		Assert.Same( disable, await fixture.Rig.Submit( "modules.disable", arguments: new[] { "sample" }, id: controlId ) );
		Assert.Equal( CommandStatus.Conflict, (await fixture.Rig.Submit( "sample.run", id: controlId )).Status );
		release.SetResult( true );
		Assert.Equal( CommandStatus.Unknown, (await active).Status );
		Assert.Equal( 1, calls );
		Assert.Equal( 1, module.Stops );
	}
}
