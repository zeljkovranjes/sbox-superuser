using SboxSuperuser.Core.Props;
using SboxSuperuser.Engine.Props;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class PropCommandTests
{
	private sealed class World : IPropWorld
	{
		internal readonly List<Guid> Removed = new();
		public int Remove( IReadOnlyList<Guid> objects ) { Removed.AddRange( objects ); return objects.Count; }
	}

	[Fact]
	public async Task PlayersShareAndCleanUpTheirOwnStaffCleanUpAnyone()
	{
		using var rig = new DispatcherRig();
		rig.Players.Add( new CommandPlayer( DispatcherRig.UserId, "User" ) );
		var props = new PropOwnership();
		var world = new World();
		using var commands = new PropCommands( rig.Registry, props, world, rig.Dispatcher.CanUse );
		var mine = Guid.NewGuid();
		var theirs = Guid.NewGuid();
		props.Record( mine, DispatcherRig.UserId );
		props.Record( theirs, DispatcherRig.TargetId );

		Assert.Equal( CommandStatus.Success, (await rig.Submit( "props.share", DispatcherRig.User, new[] { DispatcherRig.TargetId } )).Status );
		Assert.True( props.CanTouch( DispatcherRig.TargetId, mine, false ) );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "props.cleanup", DispatcherRig.User, new[] { DispatcherRig.TargetId } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Submit( "props.cleanup", DispatcherRig.User )).Status );
		Assert.Equal( new[] { mine }, world.Removed );
		Assert.Null( props.OwnerOf( mine ) );

		Assert.Equal( CommandStatus.Success, (await rig.Submit( "props.cleanup", arguments: new[] { DispatcherRig.TargetId } )).Status );
		Assert.Contains( theirs, world.Removed );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "props.cleanupall", DispatcherRig.User )).Status );
	}
}
