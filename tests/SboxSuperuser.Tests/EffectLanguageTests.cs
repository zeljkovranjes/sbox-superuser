using SboxSuperuser.Core.Effects;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class EffectLanguageTests
{
	private static readonly EffectColor[] Waw = EffectSymbols.DefaultPalette;

	[Fact]
	public void ASingleColorLineColorsEveryLetter()
	{
		var glyphs = Render( "red", "ab" );

		Assert.Equal( new[] { "a", "b" }, glyphs.Select( glyph => glyph.Text ) );
		Assert.All( glyphs, glyph => Assert.Equal( EffectSymbols.Colors[ "red" ], glyph.Color ) );
		Assert.Equal( EffectSymbols.Colors[ "cyan" ], Render( "#00ffff", "a" )[ 0 ].Color );
	}

	[Fact]
	public void EveryPresetAndTemplateCompiles()
	{
		foreach ( var preset in EffectPresets.All.Concat( EffectPresets.Templates ) )
		{
			var compiled = EffectCompiler.Compile( preset.Source );
			Assert.True( compiled.Succeeded, $"{preset.Id}: {string.Join( "; ", compiled.Diagnostics.Select( item => item.Message ) )}" );
		}
	}

	[Fact]
	public void EveryFunctionHasHelpAndAnImplementation()
	{
		var documented = EffectSymbols.All.Where( symbol => symbol.Kind == EffectSymbolKind.Function ).Select( symbol => symbol.Name );
		Assert.Equal( documented.OrderBy( name => name ), EffectFunctions.All.Keys.OrderBy( name => name ) );
		Assert.Equal( EffectSlots.Names, EffectSymbols.All.Where( symbol => symbol.Kind != EffectSymbolKind.Function ).Select( symbol => symbol.Name ) );
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( 150 )]
	[InlineData( 420 )]
	[InlineData( 999 )]
	public void RainMatchesWaw( int time )
	{
		var glyphs = Render( Preset( "rain" ), "[rain]", time );

		for ( var i = 0; i < glyphs.Count; i++ ) Assert.Equal( Waw[ (time / 100 + i) % 5 ], glyphs[ i ].Color );
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( 300 )]
	[InlineData( 1250 )]
	[InlineData( 4800 )]
	public void CycleMatchesWaw( int time )
	{
		const string text = "cycle";
		var glyphs = Render( Preset( "cycl" ), text, time );

		var length = text.Length;
		var baseColor = time / (length * 100) % 5;
		var edge = time / 100 % length;
		for ( var i = 0; i < length; i++ ) Assert.Equal( Waw[ i < edge ? baseColor : (baseColor + 4) % 5 ], glyphs[ i ].Color );
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( 400 )]
	[InlineData( 785 )]
	public void ScannerMatchesWaw( int time )
	{
		const string text = "scanner";
		var glyphs = Render( Preset( "cyln" ), text, time );

		var eye = (int)Math.Floor( (Math.Cos( time * 0.004 ) + 1) * 0.5 * text.Length );
		for ( var i = 0; i < text.Length; i++ ) Assert.Equal( EffectSymbols.Colors[ i == eye ? "red" : "white" ], glyphs[ i ].Color );
	}

	[Theory]
	[InlineData( 0, "o..." )]
	[InlineData( 75, ".o.." )]
	[InlineData( 225, "...o" )]
	[InlineData( 300, "...o" )]
	[InlineData( 375, "..o." )]
	[InlineData( 525, "o..." )]
	public void BouncingDotMatchesWaw( int time, string expected )
	{
		Assert.Equal( expected, string.Concat( Render( Preset( "dots" ), "....", time ).Select( glyph => glyph.Text ) ) );
	}

	[Fact]
	public void BouncingPlusReplacesStars()
	{
		Assert.Equal( "+---", string.Concat( Render( Preset( "stars" ), "****" ).Select( glyph => glyph.Text ) ) );
		Assert.Equal( "-+--", string.Concat( Render( Preset( "stars" ), "****", 75 ).Select( glyph => glyph.Text ) ) );
	}

	[Fact]
	public void IfElseVariablesAndListsWork()
	{
		var glyphs = Render( """
			// first letter red, the rest from a list
			colors = [blue, green]
			if i == 0 {
				color = red
			} else if i == 1 {
				color = colors[0]
			} else {
				color = colors[i]
				bold = true
			}
			""", "abc" );

		Assert.Equal( EffectSymbols.Colors[ "red" ], glyphs[ 0 ].Color );
		Assert.Equal( EffectSymbols.Colors[ "blue" ], glyphs[ 1 ].Color );
		Assert.Equal( EffectSymbols.Colors[ "blue" ], glyphs[ 2 ].Color );
		Assert.True( glyphs[ 2 ].Bold );
	}

	[Fact]
	public void HiddenAlphaAndCharShapeTheOutput()
	{
		var glyphs = Render( "hidden = i == 1\nalpha = 0.5\nchar = upper(ch)", "abc" );

		Assert.Equal( new[] { "A", "C" }, glyphs.Select( glyph => glyph.Text ) );
		Assert.All( glyphs, glyph => Assert.Equal( 0.5f, glyph.Color.A, 3 ) );
	}

	[Theory]
	[InlineData( "color = rde", "did you mean red?" )]
	[InlineData( "color = 5", "color needs a color" )]
	[InlineData( "color = floor(1, 2)", "floor takes 1 value" )]
	[InlineData( "t = 5", "t is an input" )]
	[InlineData( "color = mix(red, blue", "never closed" )]
	[InlineData( "color = red blue", "own line" )]
	[InlineData( "color =", "a value is missing" )]
	[InlineData( "5", "a line on its own sets the color" )]
	[InlineData( "color = red + blue", "use mix" )]
	[InlineData( "if i { color = red", "never closed" )]
	[InlineData( "color = \"red", "never closed" )]
	[InlineData( "color = #12", "is not a color" )]
	[InlineData( "color = sparkle(red)", "no function called sparkle" )]
	[InlineData( "floor = 3", "built-in function" )]
	public void MistakesExplainThemselves( string source, string expected )
	{
		var compiled = EffectCompiler.Compile( source );

		Assert.False( compiled.Succeeded );
		Assert.Contains( compiled.Diagnostics, diagnostic => diagnostic.Message.Contains( expected ) );
	}

	[Fact]
	public void EveryBrokenLineIsReportedWithItsPosition()
	{
		const string source = "color = rde\nalpha = 1\nbold = nope + \"x\" - 2";
		var compiled = EffectCompiler.Compile( source );

		Assert.Equal( new[] { 1, 3 }, compiled.Diagnostics.Select( diagnostic => diagnostic.Line( source ) ).Distinct() );
		var first = compiled.Diagnostics[ 0 ];
		Assert.Equal( "rde", source.Substring( first.Start, first.Length ) );
	}

	[Fact]
	public void RuntimeProblemsFailTheFrameInsteadOfThrowing()
	{
		var program = EffectCompiler.Compile( "amount = i > 1 ? \"lots\" : 0.5\ncolor = mix(red, blue, amount)" ).Program;
		Assert.NotNull( program );
		var output = new List<EffectGlyph>();

		Assert.False( program.Render( Input( "abcd" ), output ) );
		Assert.Empty( output );
		Assert.Contains( "mix needs a number", program.LastError.Message );
		Assert.True( program.Render( Input( "ab" ), output ) );
	}

	[Fact]
	public void AnimationIsDetectedFromTheClock()
	{
		Assert.True( EffectCompiler.Compile( Preset( "rain" ) ).Program.IsAnimated );
		Assert.False( EffectCompiler.Compile( "color = palette[i]" ).Program.IsAnimated );
	}

	[Fact]
	public void HostileInputNeverThrows()
	{
		var random = new Random( 7 );
		var pieces = new[] { "color", "=", "(", ")", "[", "]", "{", "}", "if", "else", "red", "1", "\"x", "\"y\"", "#ff", "+", "-", "*", "/", "%", "?", ":", ",", "\n", ";", "i", "palette", "mix", "floor", "%name%", "%x%", "//", "/*", "@", "and", "not", "==" };
		for ( var round = 0; round < 400; round++ )
		{
			var source = string.Join( " ", Enumerable.Range( 0, random.Next( 1, 40 ) ).Select( _ => pieces[ random.Next( pieces.Length ) ] ) );
			var compiled = EffectCompiler.Compile( source );
			compiled.Program?.Render( Input( "test" ), new List<EffectGlyph>() );
		}

		Assert.False( EffectCompiler.Compile( new string( '(', 500 ) + "1" + new string( ')', 500 ) ).Succeeded );
		Assert.False( EffectCompiler.Compile( new string( 'a', 5000 ) ).Succeeded );
	}

	private static string Preset( string id ) => EffectPresets.Find( id ).Source;

	private static EffectInput Input( string text, double time = 0 ) =>
		new( text, "tag", time, 1, EffectValue.Of( Waw.Select( EffectValue.Of ).ToArray() ), "Player", text );

	private static List<EffectGlyph> Render( string source, string text, double time = 0 )
	{
		var compiled = EffectCompiler.Compile( source );
		Assert.True( compiled.Succeeded, string.Join( "; ", compiled.Diagnostics.Select( item => item.Message ) ) );
		var output = new List<EffectGlyph>();
		Assert.True( compiled.Program.Render( Input( text, time ), output ), compiled.Program.LastError?.Message );
		return output;
	}
}
