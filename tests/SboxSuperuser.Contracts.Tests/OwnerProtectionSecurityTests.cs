using SboxSuperuser.Core.Permissions;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class OwnerProtectionSecurityTests
{
	private const string FirstOwner = "76561198000000001";
	private const string SecondOwner = "76561198000000002";

	[Fact]
	public void PolicyCannotCreateAGroupAboveReservedOwnerProtection()
	{
		Assert.Throws<ArgumentException>( () => new GroupPolicy( "escalated", protectionLevel: TargetProtection.OwnerLevel + 1 ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => PolicyChange.SetGroupProtection( "admin", TargetProtection.OwnerLevel ) );

		var owners = new OwnerSet( new[] { FirstOwner, SecondOwner } );
		var policy = PolicySnapshot.CreateDefault().Graph;
		var decision = TargetProtection.Check( policy, owners, FirstOwner, false, SecondOwner );

		Assert.False( decision.IsAllowed );
		Assert.Equal( TargetProtection.OwnerLevel, decision.ActorLevel );
		Assert.Equal( TargetProtection.OwnerLevel, decision.TargetLevel );
	}
}
