using SboxSuperuser.Core.Alts;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class IpAddressTests
{
	[Theory]
	[InlineData( "203.0.113.7", "203.0.113.7" )]
	[InlineData( "203.0.113.7:27015", "203.0.113.7" )]
	[InlineData( "[2001:DB8::1]:27015", "2001:db8::1" )]
	[InlineData( "127.0.0.1", null )]
	[InlineData( "::1", null )]
	[InlineData( "76561198000000001", null )]
	[InlineData( "300.1.1.1", null )]
	[InlineData( "local", null )]
	public void RecognisesAddressesAndIgnoresEverythingElse( string address, string expected ) =>
		Assert.Equal( expected, IpAddress.Normalize( address ) );
}
