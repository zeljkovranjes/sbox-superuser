using SboxSuperuser.Core.Commands;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class NativeSettingRouteTests
{
	[Theory]
	[InlineData( "get" )]
	[InlineData( "set" )]
	[InlineData( "reset" )]
	public void BindingNameSelectsItsOwnPermissionCheckedDescriptor( string operation )
	{
		var route = CommandRoute.Resolve( new[] { "settings", operation, "game.capacity", "12" } );
		Assert.Equal( "settings." + operation + ".game.capacity", route.Command );
		Assert.Equal( new[] { "12" }, route.Arguments );
	}

	[Fact]
	public void NativeCommandInjectionNeverBecomesARoute()
	{
		Assert.Throws<ArgumentException>( () => CommandRoute.Resolve( new[] { "settings", "set", "capacity;quit", "12" } ) );
		var listing = CommandRoute.Resolve( new[] { "settings", "list", "2" } );
		Assert.Equal( "settings.list", listing.Command );
		Assert.Equal( new[] { "2" }, listing.Arguments );
	}
}
