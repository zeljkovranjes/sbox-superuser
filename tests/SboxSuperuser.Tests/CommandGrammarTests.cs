using SboxSuperuser.Core.Commands;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class CommandGrammarTests
{
	[Theory]
	[InlineData( "\"unfinished" )]
	[InlineData( "'unfinished" )]
	[InlineData( "name\"tail\"" )]
	[InlineData( "\"name\"tail" )]
	[InlineData( "\"bad\\q\"" )]
	[InlineData( "one\ntwo" )]
	[InlineData( "one\0two" )]
	public void MalformedTextNeverReturnsPartialTokens( string text )
	{
		var result = CommandTokenizer.Tokenize( "valid " + text );
		Assert.NotNull( result.Error );
		Assert.Empty( result.Tokens );
	}

	[Fact]
	public void QuotesAndEscapesPreserveLiteralTokensIncludingEmptyValues()
	{
		var result = CommandTokenizer.Tokenize( "  ban\t\"Jane Doe\" '' \"quoted \\\"reason\\\"\" 'a\\\\b'" );
		Assert.Null( result.Error );
		Assert.Equal( new[] { "ban", "Jane Doe", "", "quoted \"reason\"", "a\\b" }, result.Tokens );
	}

	[Fact]
	public void InputLimitsRejectBeforeReturningWork()
	{
		foreach ( var input in new[] { new string( ' ', 4097 ), new string( 'x', 513 ), string.Join( ' ', Enumerable.Repeat( "x", 65 ) ) } )
		{
			Assert.NotNull( CommandTokenizer.Tokenize( input ).Error );
		}

		Assert.Null( CommandTokenizer.Tokenize( new string( 'x', 512 ) ).Error );
		Assert.Null( CommandTokenizer.Tokenize( string.Join( ' ', Enumerable.Repeat( "x", 64 ) ) ).Error );
	}

	[Theory]
	[InlineData( "0s" )]
	[InlineData( "-1h" )]
	[InlineData( "+1h" )]
	[InlineData( "01h" )]
	[InlineData( "1.5h" )]
	[InlineData( "1h30m" )]
	[InlineData( "1" )]
	[InlineData( "forever" )]
	[InlineData( "9223372036854775807w" )]
	public void InvalidDurationsNeverBecomePermanentOrOverflow( string input )
	{
		Assert.False( ScalarConversion.Duration( input, out _ ) );
	}

	[Fact]
	public void IntegerBoundsDoNotRoundLargeIntegersOrOverflowOnWideBounds()
	{
		Assert.True( ScalarConversion.Integer( "9223372036854775807", -1e300, 1e300, out var maximum ) );
		Assert.Equal( long.MaxValue, maximum );
		Assert.False( ScalarConversion.Integer( "9223372036854775808", null, null, out _ ) );
		Assert.False( ScalarConversion.Integer( "9007199254740993", null, 9007199254740992d, out _ ) );
		Assert.False( ScalarConversion.Integer( "-9223372036854775808", 0, null, out _ ) );
		Assert.False( ScalarConversion.Integer( "1", 1.1, null, out _ ) );
		Assert.True( ScalarConversion.Integer( "2", 1.1, 2.9, out _ ) );
		Assert.False( ScalarConversion.Integer( "9223372036854775807", 9223372036854775808d, null, out _ ) );
	}

	[Fact]
	public void ArbitraryMalformedTextIsBoundedAndNeverThrows()
	{
		var random = new Random( 8 );
		const string alphabet = "a '\"\\\t\r\n0";
		for ( var iteration = 0; iteration < 2000; iteration++ )
		{
			var text = new string( Enumerable.Range( 0, random.Next( 200 ) ).Select( _ => alphabet[ random.Next( alphabet.Length ) ] ).ToArray() );
			var result = CommandTokenizer.Tokenize( text );
			Assert.True( result.Tokens.Count <= 64 );
			if ( result.Error is not null )
			{
				Assert.Empty( result.Tokens );
			}
		}
	}
}
