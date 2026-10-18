using SboxSuperuser.Engine.NativeSettings;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class NativeReadbackTests
{
	[Fact]
	public void NativeValuesOutsideWriteBoundsRemainInspectable()
	{
		var binding = new NativeSettingDescriptor( "capacity", "game_capacity", "Capacity", "game.capacity",
			CommandArgumentKind.Integer, "8", minimum: 1, maximum: 32 );
		Assert.Null( NativeValue.Normalize( binding, "99" ) );
		Assert.Equal( "99", NativeValue.Normalize( binding, "99", validateConstraints: false ) );
		Assert.Null( NativeValue.Normalize( binding, "not-an-integer", validateConstraints: false ) );
	}
}
