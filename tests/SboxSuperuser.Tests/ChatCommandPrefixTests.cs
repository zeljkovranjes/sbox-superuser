using SboxSuperuser.Core.Commands;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class ChatCommandPrefixTests
{
	[Theory]
	[InlineData( "/admin", true )]
	[InlineData( "/admin help", true )]
	[InlineData( "/admin\thelp", true )]
	[InlineData( "/administrator", false )]
	[InlineData( "/admin-extra", false )]
	[InlineData( "!admin help", false )]
	[InlineData( "hello /admin", false )]
	[InlineData( " /admin help", false )]
	[InlineData( "/Admin help", false )]
	[InlineData( null, false )]
	public void OnlyExactSlashRootWithTokenBoundaryIsOwned( string message, bool expected )
	{
		Assert.Equal( expected, new ChatCommandPrefix( "admin" ).Owns( message ) );
	}

	[Fact]
	public void EmptyRootPayloadRoutesToHelpAndQuotedValuesRemainWhole()
	{
		var prefix = new ChatCommandPrefix( "operators" );
		Assert.False( prefix.Owns( "/admin help" ) );
		Assert.Equal( "help", CommandRoute.Resolve( prefix.Parse( "/operators" ).Tokens ).Command );
		var route = CommandRoute.Resolve( prefix.Parse( "/operators user \"Some Player\" group add moderator" ).Tokens );
		Assert.Equal( "user.group.add", route.Command );
		Assert.Equal( new[] { "Some Player", "moderator" }, route.Arguments );
	}

	[Fact]
	public void MalformedAndOversizedAdminInputIsStillOwned()
	{
		var prefix = new ChatCommandPrefix( "admin" );
		foreach ( var message in new[] { "/admin \"unfinished", "/admin\nhelp", "/admin " + new string( 'x', 4096 ) } )
		{
			Assert.True( prefix.Owns( message ) );
			Assert.NotNull( prefix.Parse( message ).Error );
		}
	}
}
