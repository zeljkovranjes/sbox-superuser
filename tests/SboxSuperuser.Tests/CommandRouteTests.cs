using SboxSuperuser.Core.Commands;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class CommandRouteTests
{
	[Fact]
	public void AccessRoutePreservesExactTokensAndReordersOnlyTheSubject()
	{
		var route = CommandRoute.Resolve( new[] { "user", "76561198000000001", "group", "add", "moderator" } );
		Assert.Equal( "user.group.add", route.Command );
		Assert.Equal( new[] { "76561198000000001", "moderator" }, route.Arguments );
		var arbitrary = CommandRoute.Resolve( new[] { "dog", "quoted \"word\"", "" } );
		Assert.Equal( new[] { "quoted \"word\"", "" }, arbitrary.Arguments );
	}

	[Theory]
	[InlineData( "permissions", "explain", "permissions.explain" )]
	[InlineData( "group", "create", "group.create" )]
	[InlineData( "audit", "query", "audit.query" )]
	public void NaturalAndCanonicalRoutesAgree( string category, string operation, string expected )
	{
		Assert.Equal( expected, CommandRoute.Resolve( new[] { category, operation } ).Command );
		Assert.Equal( expected, CommandRoute.Resolve( new[] { expected } ).Command );
		Assert.Equal( "help", CommandRoute.Resolve( Array.Empty<string>() ).Command );
	}

	[Theory]
	[InlineData( "" )]
	[InlineData( "Admin" )]
	[InlineData( "/admin" )]
	[InlineData( "admin;quit" )]
	[InlineData( "superuser" )]
	public void InvalidOrReservedRootsAreRejected( string root )
	{
		Assert.Throws<ArgumentException>( () => CommandRootName.Validate( root ) );
	}

	[Fact]
	public void MalformedNativeTokensNeverBecomeCommandText()
	{
		Assert.Throws<ArgumentException>( () => CommandRoute.Resolve( new[] { "help;quit" } ) );
		Assert.Throws<ArgumentException>( () => CommandRoute.Resolve( new[] { "help", "line\nbreak" } ) );
		Assert.Equal( "operators", CommandRootName.Validate( "operators" ) );
	}
}
