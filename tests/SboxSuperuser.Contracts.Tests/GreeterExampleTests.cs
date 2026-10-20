using SboxSuperuser.Engine.Modules;
using SuperuserExamples.Engine;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

/// <summary>Runs dev/examples/greeter exactly as a game would: loaded by the automatic loader, used through the dispatcher.</summary>
public sealed class GreeterExampleTests
{
	private static async Task<ModuleFixture> Start()
	{
		var fixture = new ModuleFixture();
		Assert.Empty( ModuleAutoLoader.Install( fixture.Manager, new IAdminModule[] { new GreeterModule() } ) );
		Assert.Equal( CommandStatus.Success, (await fixture.Manager.StartAllAsync( CancellationToken.None )).Status );
		return fixture;
	}

	[Fact]
	public async Task TheExampleLoadsAndAnswers()
	{
		using var fixture = await Start();

		var hello = await fixture.Rig.Submit( "hello" );
		var roll = await fixture.Rig.Submit( "roll", arguments: new[] { "20" } );

		Assert.Equal( "Hello! The greeter module is running.", hello.Message );
		Assert.Equal( CommandStatus.Success, roll.Status );
		Assert.Matches( @"^You rolled ([1-9]|1[0-9]|20) \(1-20\)\.$", roll.Message );
	}

	[Fact]
	public async Task TheExampleChecksItsPermissionAndArguments()
	{
		using var fixture = await Start();

		Assert.Equal( CommandStatus.Denied, (await fixture.Rig.Submit( "hello", DispatcherRig.User )).Status );
		Assert.Equal( CommandStatus.InvalidArguments, (await fixture.Rig.Submit( "roll", arguments: new[] { "1" } )).Status );

		await fixture.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "example.greeter.use", PermissionEffect.Allow ) );
		Assert.Equal( CommandStatus.Success, (await fixture.Rig.Submit( "hello", DispatcherRig.User )).Status );
	}

	[Fact]
	public async Task TheExampleDocumentsItself()
	{
		using var fixture = await Start();

		var documentation = fixture.Rig.Registry.DocumentationOf( "example.greeter" );

		Assert.Equal( "Using the greeter", Assert.Single( documentation.Pages ).Title );
		Assert.Equal( "example.greeter.use", Assert.Single( documentation.Permissions ).Key );
	}

	[Fact]
	public async Task TheLoaderInstallsModulesInAnyOrderAndReportsBrokenOnes()
	{
		using var fixture = new ModuleFixture();
		var dependent = new TestAdminModule( "example.second", false, new ModuleDependency( "example.first", 1 ) );
		var broken = new TestAdminModule( "example.broken", false, new ModuleDependency( "example.missing", 1 ) );

		var problems = ModuleAutoLoader.Install( fixture.Manager, new IAdminModule[] { dependent, broken, new TestAdminModule( "example.first" ) } );

		var problem = Assert.Single( problems );
		Assert.StartsWith( "example.broken:", problem );
		Assert.Contains( "example.missing", problem );
		Assert.Equal( CommandStatus.Success, (await fixture.Manager.StartAllAsync( CancellationToken.None )).Status );
		Assert.Equal( ModuleState.Enabled, fixture.State( "example.second" ) );
	}
}
