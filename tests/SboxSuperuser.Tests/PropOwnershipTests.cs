using SboxSuperuser.Core.Props;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class PropOwnershipTests
{
	private static readonly Guid Crate = Guid.NewGuid();
	private static readonly Guid MapDoor = Guid.NewGuid();

	[Fact]
	public void OwnersAndFriendsTouchStaffTouchEverythingOthersNothing()
	{
		var props = new PropOwnership();
		props.Record( Crate, "alice" );
		Assert.True( props.CanTouch( "alice", Crate, false ) );
		Assert.False( props.CanTouch( "bob", Crate, false ) );
		Assert.True( props.Share( "alice", "bob" ) );
		Assert.False( props.Share( "alice", "bob" ) );
		Assert.True( props.CanTouch( "bob", Crate, false ) );
		Assert.True( props.Unshare( "alice", "bob" ) );
		Assert.False( props.CanTouch( "bob", Crate, false ) );
		Assert.False( props.CanTouch( "alice", MapDoor, false ) );
		Assert.True( props.CanTouch( "mod", MapDoor, true ) );
	}

	[Fact]
	public void FirstOwnerStaysAndForgottenObjectsBecomeMapObjects()
	{
		var props = new PropOwnership();
		props.Record( Crate, "alice" );
		props.Record( Crate, "bob" );
		Assert.Equal( "alice", props.OwnerOf( Crate ) );
		Assert.Equal( new[] { Crate }, props.ObjectsOf( "alice" ) );
		props.Forget( Crate );
		Assert.Null( props.OwnerOf( Crate ) );
		Assert.False( props.Share( "alice", "alice" ) );
	}
}
