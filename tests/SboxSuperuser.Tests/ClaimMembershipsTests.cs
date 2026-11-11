using SboxSuperuser.Core.Permissions;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class ClaimMembershipsTests
{
	private const string Player = "76561198000000005";

	[Fact]
	public void ClaimsMapToOwnerAndGroupsAndLeaveWithThePlayer()
	{
		var claims = new ClaimMemberships();
		var changes = 0;
		claims.Changed += () => changes++;
		claims.Set( Player, new[] { "Admin", "mod", "kick", "restart" } );
		Assert.False( claims.IsOwner( Player ) );
		Assert.Equal( new[] { "admin", "moderator" }, claims.GroupsOf( Player ) );
		claims.Set( Player, new[] { "superuser" } );
		Assert.True( claims.IsOwner( Player ) );
		claims.Remove( Player );
		Assert.False( claims.IsOwner( Player ) );
		Assert.Empty( claims.GroupsOf( Player ) );
		claims.Set( Player, new[] { "kick" } );
		Assert.Equal( 3, changes );
	}
}
