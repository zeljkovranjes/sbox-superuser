namespace SboxSuperuser.Core.Effects;

/// <summary>
/// Every built-in name in the effect language. The checker, the syntax highlighter, the editor's help list and the
/// generated reference all read from here, so they can never disagree.
/// </summary>
internal static class EffectSymbols
{
	// The World at War palette (^0 to ^7), plus a few extra names.
	internal static readonly IReadOnlyDictionary<string, EffectColor> Colors = new Dictionary<string, EffectColor>( StringComparer.Ordinal )
	{
		[ "black" ] = Hex( "#000000" ),
		[ "red" ] = Hex( "#FF3333" ),
		[ "green" ] = Hex( "#00FF00" ),
		[ "yellow" ] = Hex( "#FFFF00" ),
		[ "blue" ] = Hex( "#0000FF" ),
		[ "cyan" ] = Hex( "#00FFFF" ),
		[ "pink" ] = Hex( "#FF5CFF" ),
		[ "white" ] = Hex( "#FFFFFF" ),
		[ "orange" ] = Hex( "#FF8800" ),
		[ "purple" ] = Hex( "#9933FF" ),
		[ "gray" ] = Hex( "#888888" )
	};

	/// <summary>The palette a prefix uses when it sets none: the five WaW color codes ^1 to ^5.</summary>
	internal static readonly EffectColor[] DefaultPalette = { Colors[ "red" ], Colors[ "green" ], Colors[ "yellow" ], Colors[ "blue" ], Colors[ "cyan" ] };

	internal static readonly IReadOnlyList<EffectSymbol> All = new EffectSymbol[]
	{
		new( "t", EffectSymbolKind.Input, "Time in milliseconds. Every player sees the same value at the same moment.", "floor(t / 100)" ),
		new( "i", EffectSymbolKind.Input, "Which letter this is, starting at 0.", "i == 0" ),
		new( "L", EffectSymbolKind.Input, "How many letters this part has.", "i == L - 1" ),
		new( "ch", EffectSymbolKind.Input, "This letter, as text.", "ch == \"a\"" ),
		new( "part", EffectSymbolKind.Input, "Which part is drawing: \"tag\" or \"name\".", "part == \"name\"" ),
		new( "text", EffectSymbolKind.Input, "The whole text of this part.", "len(text)" ),
		new( "palette", EffectSymbolKind.Input, "The prefix's colors as a list. Indexes wrap around.", "palette[i]" ),
		new( "seed", EffectSymbolKind.Input, "A number fixed per player, for effects that differ between players.", "hash(i, seed)" ),

		new( "color", EffectSymbolKind.Output, "This letter's color. Starts white.", "color = red" ),
		new( "alpha", EffectSymbolKind.Output, "Opacity from 0 to 1. Starts at 1.", "alpha = wave(t, 1000)" ),
		new( "char", EffectSymbolKind.Output, "Replace this letter with other text.", "char = \"o\"" ),
		new( "offset", EffectSymbolKind.Output, "Shift this letter sideways, in letter widths.", "offset = 2" ),
		new( "bold", EffectSymbolKind.Output, "Draw this letter bold.", "bold = true" ),
		new( "italic", EffectSymbolKind.Output, "Draw this letter slanted.", "italic = i % 2 == 0" ),
		new( "hidden", EffectSymbolKind.Output, "Hide this letter.", "hidden = i > 3" ),

		new( "%name%", EffectSymbolKind.Placeholder, "The player's name.", "len(%name%)" ),
		new( "%prefix%", EffectSymbolKind.Placeholder, "The prefix's tag text.", "upper(%prefix%)" ),

		new( "floor", EffectSymbolKind.Function, "Round down.", "floor(t / 100)", 1, 1 ),
		new( "ceil", EffectSymbolKind.Function, "Round up.", "ceil(L / 2)", 1, 1 ),
		new( "round", EffectSymbolKind.Function, "Round to the nearest whole number.", "round(1.6)", 1, 1 ),
		new( "abs", EffectSymbolKind.Function, "Remove the minus sign.", "abs(sin(t))", 1, 1 ),
		new( "sqrt", EffectSymbolKind.Function, "Square root.", "sqrt(i)", 1, 1 ),
		new( "pow", EffectSymbolKind.Function, "Raise to a power.", "pow(2, i)", 2, 2 ),
		new( "sin", EffectSymbolKind.Function, "Sine, in radians.", "sin(t * 0.004)", 1, 1 ),
		new( "cos", EffectSymbolKind.Function, "Cosine, in radians.", "cos(t * 0.004)", 1, 1 ),
		new( "tan", EffectSymbolKind.Function, "Tangent, in radians.", "tan(t)", 1, 1 ),
		new( "min", EffectSymbolKind.Function, "The smaller of two numbers.", "min(i, 3)", 2, 2 ),
		new( "max", EffectSymbolKind.Function, "The larger of two numbers.", "max(i, 3)", 2, 2 ),
		new( "clamp", EffectSymbolKind.Function, "Keep a number between a low and a high value.", "clamp(i, 0, 5)", 3, 3 ),
		new( "lerp", EffectSymbolKind.Function, "Blend two numbers; 0 gives the first, 1 the second.", "lerp(0, 10, 0.5)", 3, 3 ),
		new( "mod", EffectSymbolKind.Function, "Remainder that never goes negative.", "mod(i - 1, L)", 2, 2 ),
		new( "sign", EffectSymbolKind.Function, "-1, 0 or 1 depending on the sign.", "sign(sin(t))", 1, 1 ),
		new( "rgb", EffectSymbolKind.Function, "A color from red, green and blue (0-255), with optional alpha.", "rgb(255, 128, 0)", 3, 4 ),
		new( "hsv", EffectSymbolKind.Function, "A color from hue (degrees), saturation and value (0-1).", "hsv(t / 10 + i * 30, 1, 1)", 3, 3 ),
		new( "mix", EffectSymbolKind.Function, "Blend two colors; 0 gives the first, 1 the second.", "mix(red, blue, i / L)", 3, 3 ),
		new( "fade", EffectSymbolKind.Function, "A color with a different opacity.", "fade(red, 0.5)", 2, 2 ),
		new( "len", EffectSymbolKind.Function, "How long some text or a list is.", "len(palette)", 1, 1 ),
		new( "upper", EffectSymbolKind.Function, "Text in capitals.", "upper(ch)", 1, 1 ),
		new( "lower", EffectSymbolKind.Function, "Text in small letters.", "lower(ch)", 1, 1 ),
		new( "at", EffectSymbolKind.Function, "The letter at a position in some text; positions wrap around.", "at(%name%, i + 1)", 2, 2 ),
		new( "pingpong", EffectSymbolKind.Function, "Counts 0, 1 ... n-1 and back down, forever.", "pingpong(t / 75, 4)", 2, 2 ),
		new( "wave", EffectSymbolKind.Function, "A smooth 0 to 1 to 0 wave lasting the given milliseconds.", "wave(t, 1000)", 2, 2 ),
		new( "hash", EffectSymbolKind.Function, "A fixed random-looking number from 0 to 1 for the inputs.", "hash(i, seed)", 1, 2 ),
	};

	private static readonly Dictionary<string, EffectSymbol> ByName = All.ToDictionary( symbol => symbol.Name, StringComparer.Ordinal );

	internal static EffectSymbol Find( string name ) => ByName.GetValueOrDefault( name );

	internal static bool IsColor( string name ) => Colors.ContainsKey( name );

	/// <summary>The closest candidate within two typos (a swapped pair counts as one), for "did you mean" hints.</summary>
	internal static string Suggest( string name, IEnumerable<string> candidates )
	{
		string best = null;
		var bestDistance = 3;
		foreach ( var candidate in candidates )
		{
			var distance = Distance( name, candidate );
			if ( distance >= bestDistance ) continue;
			best = candidate;
			bestDistance = distance;
		}
		return best;
	}

	internal static IEnumerable<string> FunctionNames => All.Where( symbol => symbol.Kind == EffectSymbolKind.Function ).Select( symbol => symbol.Name );

	// Optimal string alignment distance, ignoring case.
	private static int Distance( string a, string b )
	{
		var table = new int[ a.Length + 1, b.Length + 1 ];
		for ( var i = 0; i <= a.Length; i++ ) table[ i, 0 ] = i;
		for ( var j = 0; j <= b.Length; j++ ) table[ 0, j ] = j;
		for ( var i = 1; i <= a.Length; i++ )
		{
			for ( var j = 1; j <= b.Length; j++ )
			{
				var cost = Same( a[ i - 1 ], b[ j - 1 ] ) ? 0 : 1;
				table[ i, j ] = Math.Min( Math.Min( table[ i - 1, j ] + 1, table[ i, j - 1 ] + 1 ), table[ i - 1, j - 1 ] + cost );
				if ( i > 1 && j > 1 && Same( a[ i - 1 ], b[ j - 2 ] ) && Same( a[ i - 2 ], b[ j - 1 ] ) )
					table[ i, j ] = Math.Min( table[ i, j ], table[ i - 2, j - 2 ] + 1 );
			}
		}
		return table[ a.Length, b.Length ];
	}

	private static bool Same( char a, char b ) => char.ToLowerInvariant( a ) == char.ToLowerInvariant( b );

	private static EffectColor Hex( string hex ) => EffectColor.TryParseHex( hex, out var color ) ? color : EffectColor.White;
}
