using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Modules;
using SuperuserExamples.Engine;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

/// <summary>Runs dev/examples/reports as a game would, including the menu windows each viewer receives.</summary>
public sealed class ReportsExampleTests
{
	private static async Task<(ModerationRig Rig, ModuleManager Manager)> Start()
	{
		var rig = new ModerationRig();
		var manager = new ModuleManager( rig.Rig.Registry, rig.Rig.Policy, () => rig.Rig.Active, rig.Rig.Errors.Add );
		Assert.Empty( ModuleAutoLoader.Install( manager, new IAdminModule[] { new ReportsModule(), new GreeterModule() } ) );
		Assert.Equal( CommandStatus.Success, (await manager.StartAllAsync( CancellationToken.None )).Status );
		await rig.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "example.reports.file", PermissionEffect.Allow ) );
		return (rig, manager);
	}

	private static Task<CommandResult> As( ModerationRig rig, ActorIdentity actor, string command, params string[] arguments ) =>
		rig.Rig.Submit( command, actor, arguments );

	[Fact]
	public async Task AReportGoesFromOpenToClaimedToClosed()
	{
		var (rig, manager) = await Start();
		using var _ = rig;
		using var __ = manager;

		var filed = await As( rig, DispatcherRig.User, "report", DispatcherRig.TargetId, "keeps", "blocking", "the", "spawn" );
		Assert.Equal( "Report #1 filed. Staff will look at it.", filed.Message );

		var open = await As( rig, DispatcherRig.Owner, "reports.list" );
		Assert.Equal( "1 open report.", open.Message );
		Assert.Contains( "keeps blocking the spawn", open.Data[ "row1" ] );

		Assert.Equal( CommandStatus.Success, (await As( rig, DispatcherRig.Owner, "reports.claim", "1" )).Status );
		Assert.Contains( "claimed by", (await As( rig, DispatcherRig.Owner, "reports.list", "claimed" )).Data[ "row1" ] );

		var closed = await As( rig, DispatcherRig.Owner, "reports.close", "1", "resolved", "warned", "them" );
		Assert.Equal( "Report #1 closed as resolved.", closed.Message );
		Assert.Contains( "closed as resolved (warned them)", (await As( rig, DispatcherRig.User, "reports.mine" )).Data[ "row1" ] );

		Assert.Equal( "Deleted 1 report.", (await As( rig, DispatcherRig.Owner, "reports.clear", "closed" )).Message );
	}

	[Fact]
	public async Task EachStepNeedsItsOwnPermissionAndValidArguments()
	{
		var (rig, manager) = await Start();
		using var _ = rig;
		using var __ = manager;
		await As( rig, DispatcherRig.User, "report", DispatcherRig.TargetId, "spam" );

		Assert.Equal( CommandStatus.Denied, (await As( rig, DispatcherRig.User, "reports.list" )).Status );
		Assert.Equal( CommandStatus.Denied, (await As( rig, DispatcherRig.User, "reports.claim", "1" )).Status );
		Assert.Equal( CommandStatus.InvalidArguments, (await As( rig, DispatcherRig.User, "report", DispatcherRig.UserId, "me" )).Status );
		Assert.Equal( CommandStatus.InvalidArguments, (await As( rig, DispatcherRig.Owner, "reports.close", "1", "maybe" )).Status );
		Assert.Equal( CommandStatus.NotFound, (await As( rig, DispatcherRig.Owner, "reports.claim", "9" )).Status );
	}

	[Fact]
	public async Task TheModuleDocumentsItselfOnFourPages()
	{
		var (rig, manager) = await Start();
		using var _ = rig;
		using var __ = manager;

		var documentation = rig.Rig.Registry.DocumentationOf( "example.reports" );

		Assert.Equal( new[] { "Overview", "Reporting a player", "Handling reports", "Setting up" }, documentation.Pages.Select( page => page.Title ) );
		Assert.Equal( 4, documentation.Permissions.Count );
	}

	[Fact]
	public async Task EachViewerGetsOnlyTheMenuWindowsTheyCanUse()
	{
		var (rig, manager) = await Start();
		using var _ = rig;
		using var __ = manager;
		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, registry: rig.Rig.Registry );

		var owner = await queries.QueryAsync( () => DispatcherRig.Owner, new AdminQuery( AdminQueryKind.MenuWindows ), Guid.NewGuid(), CancellationToken.None );
		var player = await queries.QueryAsync( () => DispatcherRig.User, new AdminQuery( AdminQueryKind.MenuWindows ), Guid.NewGuid(), CancellationToken.None );

		Assert.Equal( new[] { "Greeter", "My reports", "Reports" }, owner.MenuWindows.Select( window => window.Title ) );
		var mine = Assert.Single( player.MenuWindows );
		Assert.Equal( "My reports", mine.Title );
		Assert.Contains( mine.Elements, element => element.Kind == AdminMenuElementKind.Form && element.Command == "report" );
		Assert.Contains( mine.Elements, element => element.Kind == AdminMenuElementKind.Live && element.Command == "reports.mine" );
	}

	[Fact]
	public void WindowsRejectBadInput()
	{
		using var rig = new ModerationRig();
		var scope = new CommandRegistrationScope( rig.Rig.Registry, "example.bad" );

		Assert.Throws<ArgumentException>( () => scope.AddMenuWindow( "Bad", window => window.Button( "Go", "Not A Command" ) ) );
		Assert.Throws<ArgumentException>( () => scope.AddMenuWindow( new string( 'x', 60 ), window => window.Text( "too long a title" ) ) );
	}
	[Fact]
	public async Task WindowsGoUnderTheirTabButNeverTheSuperuserOne()
	{
		using var rig = new ModerationRig();
		var scope = new CommandRegistrationScope( rig.Rig.Registry, "example.tabs" );
		scope.RegisterCommand( new CommandDescriptor( "tabs.read", "Read", "test.use", scope.ModuleId ),
			(_, _) => Task.FromResult( new CommandResult( CommandStatus.Success, "Done" ) ) );
		foreach ( var reserved in new[] { "Superuser", "superuser tools", "My SUPERUSER" } )
			Assert.Throws<ArgumentException>( () => scope.AddMenuWindow( "Window", window => window.Text( "x" ), reserved ) );
		scope.AddMenuWindow( "Default", window => window.Live( "tabs.read" ) );
		scope.AddMenuWindow( "Staff", window => window.Live( "tabs.read" ), "Moderation" );
		scope.AddMenuWindow( "Economy", window => window.Live( "tabs.read" ), " Shop " );
		scope.Commit();
		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, registry: rig.Rig.Registry );
		var reply = await queries.QueryAsync( () => DispatcherRig.Owner, new AdminQuery( AdminQueryKind.MenuWindows ), Guid.NewGuid(), CancellationToken.None );
		Assert.Equal( new[] { "Moderation", "Modules", "Shop" }, reply.MenuWindows.Where( window => window.ModuleId == "example.tabs" ).Select( window => window.Menu ).Order() );
	}

	[Fact]
	public void SettingsCopyDefaultsAndRejectInvalidInput()
	{
		var defaults = new[] { "16", "4" };
		var window = new AdminMenuWindow( "Limits" ).Settings( "Spawn limits", "limits.configure", "limits.status", defaults );
		defaults[0] = "999";
		var element = Assert.Single( window.Elements );
		Assert.Equal( AdminMenuElementKind.Settings, element.Kind );
		Assert.Equal( "limits.status", element.StatusCommand );
		Assert.Equal( "16", element.Arguments[0] );
		Assert.Throws<ArgumentException>( () => window.Settings( "Limits", "limits.configure", "Bad Command" ) );
		Assert.Throws<ArgumentException>( () => window.Settings( "Bad\nTitle", "limits.configure", "limits.status" ) );
		Assert.Throws<ArgumentException>( () => window.Settings( "Limits", "limits.configure", "limits.status", new string( 'x', 2048 ) ) );
	}

	[Fact]
	public async Task SettingsRequireBothReadAndWritePermission()
	{
		using var rig = new ModerationRig();
		using var scope = new CommandRegistrationScope( rig.Rig.Registry, "example.settings" );
		foreach ( var name in new[] { "limits.read", "limits.write" } )
		{
			scope.RegisterPermission( new PermissionDescriptor( name, "Settings permission" ) );
			scope.RegisterCommand( new CommandDescriptor( name, "Settings command", name, scope.ModuleId ),
				(_, _) => Task.FromResult( new CommandResult( CommandStatus.Success, "Done" ) ) );
		}
		scope.AddMenuWindow( "Settings", window => window.Settings( "Limits", "limits.write", "limits.read", "1" ) );
		scope.Commit();
		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, registry: rig.Rig.Registry );
		async Task<bool> Visible() => (await queries.QueryAsync( () => DispatcherRig.User,
			new AdminQuery( AdminQueryKind.MenuWindows ), Guid.NewGuid(), CancellationToken.None )).MenuWindows.Any( window => window.Title == "Settings" );
		Assert.False( await Visible() );
		await rig.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "limits.write", PermissionEffect.Allow ) );
		Assert.False( await Visible() );
		await rig.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "limits.read", PermissionEffect.Allow ) );
		Assert.True( await Visible() );
		await rig.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "limits.write", PermissionEffect.Deny ) );
		Assert.False( await Visible() );
	}

}
