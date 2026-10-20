using Sandbox;
using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.UI;

/// <summary>Colors effect scripts from the language's own tokens, so the editor and the docs agree with the compiler.</summary>
internal static class EffectHighlighter
{
	internal static readonly Vector4 Plain = Rgb( 0xD4D4D4 );
	private static readonly Vector4 Comment = Rgb( 0x6A9955 );
	private static readonly Vector4 Number = Rgb( 0xB5CEA8 );
	private static readonly Vector4 Text = Rgb( 0xCE9178 );
	private static readonly Vector4 HexColor = Rgb( 0xD7BA7D );
	private static readonly Vector4 Placeholder = Rgb( 0x4EC9B0 );
	private static readonly Vector4 Keyword = Rgb( 0xC586C0 );
	private static readonly Vector4 Function = Rgb( 0xDCDCAA );
	private static readonly Vector4 BuiltIn = Rgb( 0x569CD6 );
	private static readonly Vector4 Variable = Rgb( 0x9CDCFE );
	private static readonly Vector4 Invalid = Rgb( 0xF44747 );

	/// <summary>Spans for every token, split at line breaks; line and column count from 0.</summary>
	internal static List<HighlightSpan> Spans( string source, IReadOnlyList<EffectToken> tokens )
	{
		var spans = new List<HighlightSpan>();
		var starts = LineStarts( source );
		for ( var index = 0; index < tokens.Count; index++ )
		{
			var token = tokens[ index ];
			if ( token.Kind == EffectTokenKind.End || token.Length == 0 ) continue;
			var (color, swatch) = Style( token, index + 1 < tokens.Count ? tokens[ index + 1 ] : default );
			AddSplit( spans, starts, source, token.Start, token.Length, color, swatch );
		}
		return spans;
	}

	internal static int[] LineStarts( string source )
	{
		var starts = new List<int> { 0 };
		for ( var index = 0; index < source.Length; index++ )
			if ( source[ index ] == '\n' ) starts.Add( index + 1 );
		return starts.ToArray();
	}

	internal static int LineOf( int[] starts, int offset )
	{
		var line = Array.BinarySearch( starts, offset );
		return line >= 0 ? line : ~line - 1;
	}

	private static (Vector4 Color, Vector4? Swatch) Style( EffectToken token, EffectToken next )
	{
		switch ( token.Kind )
		{
			case EffectTokenKind.Comment: return (Comment, null);
			case EffectTokenKind.Number: return (Number, null);
			case EffectTokenKind.Text: return (Text, null);
			case EffectTokenKind.Placeholder: return (Placeholder, null);
			case EffectTokenKind.Keyword: return (Keyword, null);
			case EffectTokenKind.Invalid: return (Invalid, null);
			case EffectTokenKind.Color:
				return (HexColor, EffectColor.TryParseHex( token.Text, out var hex ) ? ToVector( hex ) : null);
			case EffectTokenKind.Identifier:
				if ( next.Is( "(" ) ) return (EffectFunctions.All.ContainsKey( token.Text ) ? Function : Plain, null);
				if ( EffectSymbols.Colors.TryGetValue( token.Text, out var named ) ) return (Variable, ToVector( named ));
				return (EffectSymbols.Find( token.Text ) is null ? Variable : BuiltIn, null);
			default:
				return (Plain, null);
		}
	}

	private static void AddSplit( List<HighlightSpan> spans, int[] starts, string source, int start, int length, Vector4 color, Vector4? swatch )
	{
		var end = start + length;
		while ( start < end )
		{
			var line = LineOf( starts, start );
			var lineEnd = line + 1 < starts.Length ? starts[ line + 1 ] - 1 : source.Length;
			var pieceEnd = Math.Min( end, lineEnd );
			if ( pieceEnd > start ) spans.Add( new HighlightSpan( line, start - starts[ line ], pieceEnd - start, color, swatch ) );
			start = pieceEnd + 1;
		}
	}

	private static Vector4 ToVector( EffectColor color ) => new( color.R, color.G, color.B, 1 );

	private static Vector4 Rgb( int hex ) => new( ((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, 1 );
}
