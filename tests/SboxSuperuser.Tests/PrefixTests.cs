using System.Text.Json;
using SboxSuperuser.Core.Effects;
using SboxSuperuser.Core.Prefixes;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class PrefixTests
{
	private const string Player = "76561198000000001";

	private static PrefixCatalog Catalog() => PrefixCatalog.Empty
		.Save( PrefixDefinition.Create( "vip", "VIP", "[VIP]" ) )
		.Save( PrefixDefinition.Create( "staff", "Staff", "[STAFF]", tagStyle: new PrefixStyle( PrefixStyleKind.Preset, "rain" ) ) )
		.Save( PrefixDefinition.Create( "mod", "Moderator", "[MOD]" ) )
		.AssignGroup( "moderator", "mod" )
		.AssignGroup( "admin", "staff" );

	[Fact]
	public void APlayersOwnPrefixBeatsTheirGroups()
	{
		var catalog = Catalog().AssignUser( Player, "vip" );

		Assert.Equal( "vip", catalog.Resolve( Player, new[] { new GroupRank( "admin", 50 ) } ).Id );
	}

	[Fact]
	public void TheHighestProtectionGroupWins()
	{
		var catalog = Catalog();

		Assert.Equal( "staff", catalog.Resolve( Player, new[] { new GroupRank( "moderator", 10 ), new GroupRank( "admin", 50 ), new GroupRank( "player", 0 ) } ).Id );
		Assert.Equal( "mod", catalog.Resolve( Player, new[] { new GroupRank( "moderator", 10 ) } ).Id );
		Assert.Null( catalog.Resolve( Player, new[] { new GroupRank( "player", 0 ) } ) );
	}

	[Fact]
	public void DeletingAPrefixClearsItsAssignments()
	{
		var catalog = Catalog().AssignUser( Player, "staff" ).Delete( "staff" );

		Assert.Null( catalog.Find( "staff" ) );
		Assert.False( catalog.Groups.ContainsKey( "admin" ) );
		Assert.Empty( catalog.Users );
	}

	[Fact]
	public void TheCatalogSurvivesStorage()
	{
		var catalog = Catalog().AssignUser( Player, "vip" ).Save( PrefixDefinition.Create( "fancy", "Fancy", "*", "[%prefix%] %name%",
			new PrefixStyle( PrefixStyleKind.Script, "color = palette[i]" ), PrefixStyle.Create( PrefixStyleKind.Code, "game.fire" ), new[] { "#FF0000", "#0f0" } ) );

		using var json = JsonDocument.Parse( PrefixCodec.WriteState( catalog ) );
		var read = PrefixCodec.ReadState( json.RootElement );

		Assert.Equal( PrefixCodec.WriteState( catalog ), PrefixCodec.WriteState( read ) );
		var fancy = read.Find( "fancy" );
		Assert.Equal( new[] { "#ff0000", "#00ff00" }, fancy.Palette );
		Assert.Equal( PrefixStyleKind.Code, fancy.NameStyle.Kind );
		Assert.Equal( "vip", read.Users[ Player ] );
	}

	[Fact]
	public void SnapshotsCarryOnlyPrefixesAndOnlinePlayers()
	{
		var snapshot = new PrefixSnapshot( true, true, Catalog().Prefixes, new Dictionary<string, string> { [ Player ] = "mod" } );

		var read = PrefixCodec.ReadSnapshot( PrefixCodec.WriteSnapshot( snapshot ) );

		Assert.Equal( "mod", read.For( Player ).Id );
		Assert.DoesNotContain( "moderator", PrefixCodec.WriteSnapshot( snapshot ) );
		Assert.Null( PrefixSnapshot.Disabled.For( Player ) );
	}

	[Fact]
	public void AStoredPresetThatNoLongerExistsDrawsPlain()
	{
		var state = PrefixCodec.WriteState( Catalog() ).Replace( "\"rain\"", "\"retired\"" );
		using var json = JsonDocument.Parse( state );

		Assert.Equal( PrefixStyle.Plain, PrefixCodec.ReadState( json.RootElement ).Find( "staff" ).TagStyle );
	}

	[Theory]
	[InlineData( "%prefix%" )]
	[InlineData( "%name% %name%" )]
	[InlineData( "%prefix% %prefix% %name%" )]
	public void TemplatesNeedTheNameOnce( string template )
	{
		Assert.Throws<ArgumentException>( () => PrefixDefinition.Create( "bad", "Bad", "x", template ) );
	}

	[Fact]
	public void TheLineRendersTemplateTagAndName()
	{
		var prefix = PrefixDefinition.Create( "vip", "VIP", "VIP", "[%prefix%] %name%", new PrefixStyle( PrefixStyleKind.Preset, "red" ) );
		var runs = new List<LineRun>();

		new PrefixRenderer().Render( prefix, Compile( prefix.TagStyle.Source ), Compile( prefix.NameStyle.Source ), "Alice", 0, 1, runs );

		Assert.Equal( new[] { "[", "VIP", "] Alice" }, runs.Select( run => run.Text ) );
		Assert.Equal( EffectSymbols.Colors[ "red" ], runs[ 1 ].Color );
		Assert.Equal( "[VIP] Alice", prefix.Format( "Alice" ) );
	}

	[Fact]
	public void ABrokenEffectDrawsItsPartPlain()
	{
		var prefix = PrefixDefinition.Create( "vip", "VIP", "VIP" );
		var failing = EffectCompiler.Compile( "x = i > 0 ? \"a\" : 1\ncolor = mix(red, blue, x)" ).Program;
		var runs = new List<LineRun>();

		new PrefixRenderer().Render( prefix, failing, null, "Alice", 0, 1, runs );

		Assert.Equal( "VIP Alice", string.Concat( runs.Select( run => run.Text ) ) );
		Assert.All( runs, run => Assert.Equal( EffectColor.White, run.Color ) );
	}

	[Fact]
	public void AnOffsetPushesTheRestOfTheLineAlong()
	{
		var prefix = PrefixDefinition.Create( "move", "Move", "abc" );
		var runs = new List<LineRun>();

		new PrefixRenderer().Render( prefix, Compile( "offset = 3" ), null, "Al", 0, 1, runs );

		Assert.Equal( 3, runs[ 0 ].Gap );
		Assert.Equal( "abc Al", string.Concat( runs.Select( run => run.Text ) ) );
		Assert.Equal( 0, runs.Skip( 1 ).Sum( run => run.Gap ) );
	}

	private static IGlyphEffect Compile( string source ) => EffectCompiler.Compile( source ).Program;
}
