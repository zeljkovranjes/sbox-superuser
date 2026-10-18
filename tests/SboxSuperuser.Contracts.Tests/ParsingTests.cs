using System.Globalization;
using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ParsingTests
{
	private const string First = "76561198000000001";
	private const string Second = "76561198000000002";
	private static CommandDescriptor Descriptor( params CommandArgumentDescriptor[] arguments ) => new( "test.parse", "Parsing", "test.use", "test", arguments );
	private static CommandParseResult Parse( CommandArgumentDescriptor argument, string token )
		=> CommandArgumentParser.Parse( Descriptor( argument ), new[] { token }, Array.Empty<CommandPlayer>() );
	private static CommandArgumentDescriptor Arg( CommandArgumentKind kind, CommandTargetPolicy target = CommandTargetPolicy.None )
		=> new( "value", "Value", kind, targetPolicy: target );

	[Theory]
	[InlineData( CommandArgumentKind.Integer, "1.2" )]
	[InlineData( CommandArgumentKind.Integer, "1,000" )]
	[InlineData( CommandArgumentKind.Integer, " 1" )]
	[InlineData( CommandArgumentKind.Number, "NaN" )]
	[InlineData( CommandArgumentKind.Number, "Infinity" )]
	[InlineData( CommandArgumentKind.Number, "1e999" )]
	[InlineData( CommandArgumentKind.Number, "1,5" )]
	[InlineData( CommandArgumentKind.Boolean, "1" )]
	[InlineData( CommandArgumentKind.Boolean, "yes" )]
	public void InvalidTypedInputFailsWithoutValues( CommandArgumentKind kind, string input )
	{
		var result = Parse( Arg( kind ), input );
		Assert.False( result.IsSuccess );
		Assert.Null( result.Arguments );
		Assert.DoesNotContain( input, result.Error );
	}

	[Fact]
	public void ConversionUsesInvariantCultureAndCanonicalTypes()
	{
		var original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo( "fr-FR" );
			Assert.Equal( 1.5d, Parse( Arg( CommandArgumentKind.Number ), "1.5" ).Arguments.Get<double>( "value" ) );
			Assert.Equal( -23L, Parse( Arg( CommandArgumentKind.Integer ), "-23" ).Arguments.Get<long>( "value" ) );
			Assert.False( Parse( Arg( CommandArgumentKind.Boolean ), "FALSE" ).Arguments.Get<bool>( "value" ) );
			Assert.True( Parse( Arg( CommandArgumentKind.Boolean ), "True" ).Arguments.Get<bool>( "value" ) );
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[Fact]
	public void NumericBoundsAndEnumNamesAreStrict()
	{
		var number = new CommandArgumentDescriptor( "value", "Value", CommandArgumentKind.Number, minimum: 1, maximum: 5 );
		Assert.True( Parse( number, "5" ).IsSuccess );
		Assert.False( Parse( number, "5.0001" ).IsSuccess );
		var choice = new CommandArgumentDescriptor( "value", "Value", CommandArgumentKind.Enum, choices: new[] { "Calm", "Playful" } );
		Assert.Equal( "Playful", Parse( choice, "playFUL" ).Arguments.Get<string>( "value" ) );
		Assert.False( Parse( choice, "1" ).IsSuccess );
		Assert.False( Parse( choice, "Calm,Playful" ).IsSuccess );
	}

	[Theory]
	[InlineData( "1s", 1 )]
	[InlineData( "15m", 900 )]
	[InlineData( "2H", 7200 )]
	[InlineData( "1d", 86400 )]
	[InlineData( "1w", 604800 )]
	public void DurationsUseExplicitUnits( string input, int seconds )
	{
		Assert.Equal( TimeSpan.FromSeconds( seconds ), Parse( Arg( CommandArgumentKind.Duration ), input ).Arguments.Get<CommandDuration>( "value" ).Value );
	}

	[Fact]
	public void PermanentIsExplicitAndZeroIsNeverPermanent()
	{
		Assert.True( Parse( Arg( CommandArgumentKind.Duration ), "permanent" ).Arguments.Get<CommandDuration>( "value" ).IsPermanent );
		Assert.False( Parse( Arg( CommandArgumentKind.Duration ), "0" ).IsSuccess );
		Assert.False( Parse( Arg( CommandArgumentKind.Duration ), "0s" ).IsSuccess );
	}

	[Fact]
	public void QuotedNamesAndReasonsResolveToStableIdentity()
	{
		var descriptor = Descriptor( new CommandArgumentDescriptor( "target", "Target", CommandArgumentKind.Player, targetPolicy: CommandTargetPolicy.OnlinePlayer ),
			new CommandArgumentDescriptor( "reason", "Reason", CommandArgumentKind.String, isRemainder: true ) );
		var tokens = CommandTokenizer.Tokenize( "\"Jane Doe\" \"quoted reason\" more" ).Tokens;
		var parsed = CommandArgumentParser.Parse( descriptor, tokens, new[] { new CommandPlayer( First, "Jane Doe" ) } );
		Assert.Equal( First, parsed.Arguments.Get<PlayerReference>( "target" ).SteamId );
		Assert.Equal( "quoted reason more", parsed.Arguments.Get<string>( "reason" ) );
	}

	[Fact]
	public void DuplicateNamesReturnChoicesAndNoSelectedArguments()
	{
		var players = new[] { new CommandPlayer( Second, "Same" ), new CommandPlayer( First, "same" ) };
		var descriptor = Descriptor( Arg( CommandArgumentKind.Player, CommandTargetPolicy.AnyPlayer ) );
		var ambiguous = CommandArgumentParser.Parse( descriptor, new[] { "SAME" }, players );
		Assert.False( ambiguous.IsSuccess );
		Assert.Equal( new[] { First, Second }, ambiguous.PlayerChoices.Select( value => value.SteamId ) );
		Assert.True( CommandArgumentParser.Parse( descriptor, new[] { Second }, players ).IsSuccess );
		Assert.False( CommandArgumentParser.Parse( descriptor, new[] { "Sam" }, players ).IsSuccess );
	}

	[Fact]
	public void ExplicitIdsAreAuthoritativeEvenWhenTheyMatchAnotherPlayersDisplayName()
	{
		var players = new[] { new CommandPlayer( Second, First ) };
		var descriptor = Descriptor( Arg( CommandArgumentKind.Player, CommandTargetPolicy.AnyPlayer ) );
		var parsed = CommandArgumentParser.Parse( descriptor, new[] { First }, players );
		Assert.Equal( First, parsed.Arguments.Get<PlayerReference>( "value" ).SteamId );
		Assert.False( CommandArgumentParser.Parse( Descriptor( Arg( CommandArgumentKind.Player, CommandTargetPolicy.OnlinePlayer ) ), new[] { First }, players ).IsSuccess );
	}

	[Theory]
	[InlineData( "0" )]
	[InlineData( "076561198000000001" )]
	[InlineData( "18446744073709551616" )]
	[InlineData( "@all" )]
	[InlineData( "*" )]
	public void InvalidOfflineSelectorsAreNeverGuessed( string input )
	{
		Assert.False( Parse( Arg( CommandArgumentKind.Player, CommandTargetPolicy.AnyPlayer ), input ).IsSuccess );
	}

	[Fact]
	public void MissingOptionalDiffersFromExplicitEmptyAndExtrasFail()
	{
		var descriptor = Descriptor( new CommandArgumentDescriptor( "value", "Value", CommandArgumentKind.String, isOptional: true ) );
		Assert.False( CommandArgumentParser.Parse( descriptor, Array.Empty<string>(), Array.Empty<CommandPlayer>() ).Arguments.Contains( "value" ) );
		Assert.Equal( "", CommandArgumentParser.Parse( descriptor, new[] { "" }, Array.Empty<CommandPlayer>() ).Arguments.Get<string>( "value" ) );
		Assert.False( CommandArgumentParser.Parse( descriptor, new[] { "one", "extra" }, Array.Empty<CommandPlayer>() ).IsSuccess );
		Assert.False( CommandArgumentParser.Parse( Descriptor( Arg( CommandArgumentKind.String ) ), Array.Empty<string>(), Array.Empty<CommandPlayer>() ).IsSuccess );
	}

	[Fact]
	public void NativeTokensAreNeverRetokenizedAndInputLimitsStillApply()
	{
		var descriptor = Descriptor( Arg( CommandArgumentKind.String ) );
		var literal = "has \"quotes\" and spaces";
		Assert.Equal( literal, CommandArgumentParser.Parse( descriptor, new[] { literal }, Array.Empty<CommandPlayer>() ).Arguments.Get<string>( "value" ) );
		Assert.False( CommandArgumentParser.Parse( descriptor, new[] { new string( 'x', 513 ) }, Array.Empty<CommandPlayer>() ).IsSuccess );
		Assert.False( CommandArgumentParser.Parse( descriptor, Enumerable.Repeat( "x", 65 ).ToArray(), Array.Empty<CommandPlayer>() ).IsSuccess );
		Assert.False( CommandArgumentParser.Parse( descriptor, new string[] { null }, Array.Empty<CommandPlayer>() ).IsSuccess );
	}

	[Fact]
	public void SyntaxReflectsRequiredOptionalRemainderAndConstraints()
	{
		var descriptor = Descriptor( new CommandArgumentDescriptor( "count", "How many", CommandArgumentKind.Integer, minimum: 1, maximum: 5 ),
			new CommandArgumentDescriptor( "reason", "Why", CommandArgumentKind.String, isOptional: true, isRemainder: true ) );
		var syntax = CommandSyntaxBuilder.Build( descriptor );
		Assert.Equal( "test.parse <count> [reason...]", syntax.Syntax );
		Assert.Equal( "test.use", syntax.Permission );
		Assert.Contains( "minimum 1; maximum 5", syntax.Arguments[ 0 ] );
	}
}
