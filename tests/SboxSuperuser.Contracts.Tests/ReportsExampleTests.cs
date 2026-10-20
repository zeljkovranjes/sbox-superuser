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
}
