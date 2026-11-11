using SboxSuperuser.Core.Props;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Props;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class PropCleanupTests
{
	private sealed class World : IPropWorld
	{
		public int Remove( IReadOnlyList<Guid> objects ) => objects.Count;
	}

	[Fact]
	public async Task AdminsSetTheDelayStaffSeeOwnersAndTheDelaySurvivesReload()
	{
		using var rig = new ModerationRig();
		var cleanup = new HostPropCleanup( rig.Rig.Store, () => rig.Rig.Active, rig.Rig.Errors.Add );
		await cleanup.Initialization;
		var props = new PropOwnership();
		props.Record( Guid.NewGuid(), DispatcherRig.TargetId );
		using var commands = new PropCommands( rig.Rig.Registry, props, new World(), rig.Rig.Dispatcher.CanUse, cleanup: cleanup );

		Assert.Equal( TimeSpan.FromMinutes( 2 ), cleanup.Current.Delay );
		Assert.Equal( CommandStatus.Success, (await rig.Rig.Submit( "props.delay", arguments: new[] { "10m" } )).Status );
		Assert.Equal( TimeSpan.FromMinutes( 10 ), cleanup.Current.Delay );
		Assert.Equal( CommandStatus.InvalidArguments, (await rig.Rig.Submit( "props.delay", arguments: new[] { "5s" } )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Rig.Submit( "props.delay", DispatcherRig.User, new[] { "1h" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Rig.Submit( "props.delay", arguments: new[] { "permanent" } )).Status );
		Assert.Null( cleanup.Current.Delay );

		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, props: props, propCleanup: cleanup );
		var reply = await queries.QueryAsync( () => DispatcherRig.Owner, new AdminQuery( AdminQueryKind.Props ), Guid.NewGuid(), CancellationToken.None );
		Assert.Null( reply.PropCleanupSeconds );
		Assert.Equal( 1, Assert.Single( reply.PropOwners ).Props );
		var denied = await queries.QueryAsync( () => DispatcherRig.User, new AdminQuery( AdminQueryKind.Props ), Guid.NewGuid(), CancellationToken.None );
		Assert.Equal( CommandStatus.Denied, denied.Status );

		var reloaded = new HostPropCleanup( rig.Rig.Store, () => rig.Rig.Active, rig.Rig.Errors.Add );
		await reloaded.Initialization;
		Assert.Null( reloaded.Current.Delay );
		Assert.Empty( rig.Rig.Errors );
	}
}
