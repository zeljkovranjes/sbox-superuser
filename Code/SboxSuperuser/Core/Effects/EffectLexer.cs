namespace SboxSuperuser.Core.Effects;

/// <summary>
/// Splits a script into tokens. It never throws: unexpected characters become Invalid tokens with a diagnostic,
/// so the editor can still color everything around them.
/// </summary>
internal static class EffectLexer
{
	internal static readonly string[] Keywords = { "if", "else", "true", "false", "and", "or", "not" };

	// Longest operators first so "==" is not read as two "=".
	private static readonly string[] Operators =
	{
		"==", "!=", "<=", ">=", "&&", "||", "+", "-", "*", "/", "%", "<", ">", "=", "!", "?", ":", "(", ")", "[", "]", "{", "}", ",", ";"
	};

	internal static IReadOnlyList<EffectToken> Tokenize( string source, List<EffectDiagnostic> diagnostics )
	{
		var tokens = new List<EffectToken>();
		var index = 0;
		while ( index < source.Length )
		{
			var c = source[ index ];
			if ( char.IsWhiteSpace( c ) )
			{
				index++;
				continue;
			}

			var start = index;
			if ( c == '/' && Peek( source, index + 1 ) == '/' )
			{
				while ( index < source.Length && source[ index ] != '\n' ) index++;
				tokens.Add( new EffectToken( EffectTokenKind.Comment, source[ start..index ], start, index - start ) );
			}
			else if ( c == '/' && Peek( source, index + 1 ) == '*' )
			{
				var close = source.IndexOf( "*/", index + 2, StringComparison.Ordinal );
				index = close < 0 ? source.Length : close + 2;
				if ( close < 0 ) diagnostics.Add( new EffectDiagnostic( "this comment is never closed; end it with */", start, 2 ) );
				tokens.Add( new EffectToken( EffectTokenKind.Comment, source[ start..index ], start, index - start ) );
			}
			else if ( char.IsAsciiDigit( c ) || c == '.' && char.IsAsciiDigit( Peek( source, index + 1 ) ) )
			{
				while ( index < source.Length && (char.IsAsciiDigit( source[ index ] ) || source[ index ] == '.') ) index++;
				tokens.Add( new EffectToken( EffectTokenKind.Number, source[ start..index ], start, index - start ) );
			}
			else if ( c == '"' )
			{
				index = ReadText( source, index, diagnostics, tokens );
			}
			else if ( c == '#' )
			{
				index++;
				while ( index < source.Length && char.IsAsciiHexDigit( source[ index ] ) ) index++;
				var text = source[ start..index ];
				var valid = EffectColor.TryParseHex( text, out _ );
				if ( !valid ) diagnostics.Add( new EffectDiagnostic( $"\"{text}\" is not a color; use #rgb, #rrggbb or #rrggbbaa", start, index - start ) );
				tokens.Add( new EffectToken( valid ? EffectTokenKind.Color : EffectTokenKind.Invalid, text, start, index - start ) );
			}
			else if ( c == '%' && IsPlaceholder( source, index, out var end ) )
			{
				index = end;
				tokens.Add( new EffectToken( EffectTokenKind.Placeholder, source[ start..index ], start, index - start ) );
			}
			else if ( char.IsLetter( c ) || c == '_' )
			{
				while ( index < source.Length && (char.IsLetterOrDigit( source[ index ] ) || source[ index ] == '_') ) index++;
				var word = source[ start..index ];
				tokens.Add( new EffectToken( Keywords.Contains( word ) ? EffectTokenKind.Keyword : EffectTokenKind.Identifier, word, start, index - start ) );
			}
			else if ( Operators.FirstOrDefault( op => string.CompareOrdinal( source, index, op, 0, op.Length ) == 0 ) is { } op )
			{
				index += op.Length;
				tokens.Add( new EffectToken( EffectTokenKind.Operator, op, start, op.Length ) );
			}
			else
			{
				index++;
				diagnostics.Add( new EffectDiagnostic( $"\"{c}\" cannot be used here", start, 1 ) );
				tokens.Add( new EffectToken( EffectTokenKind.Invalid, c.ToString(), start, 1 ) );
			}
		}

		tokens.Add( new EffectToken( EffectTokenKind.End, "", source.Length, 0 ) );
		return tokens;
	}

	private static int ReadText( string source, int start, List<EffectDiagnostic> diagnostics, List<EffectToken> tokens )
	{
		var index = start + 1;
		while ( index < source.Length && source[ index ] != '"' && source[ index ] != '\n' )
			index += source[ index ] == '\\' && index + 1 < source.Length ? 2 : 1;
		var closed = index < source.Length && source[ index ] == '"';
		if ( closed ) index++;
		else diagnostics.Add( new EffectDiagnostic( "this text is never closed; end it with \"", start, index - start ) );
		tokens.Add( new EffectToken( EffectTokenKind.Text, source[ start..index ], start, index - start ) );
		return index;
	}

	// A placeholder is %word% with no spaces, so "a % b" stays the remainder operator.
	private static bool IsPlaceholder( string source, int start, out int end )
	{
		end = start + 1;
		while ( end < source.Length && char.IsAsciiLetter( source[ end ] ) ) end++;
		if ( end == start + 1 || end >= source.Length || source[ end ] != '%' ) return false;
		end++;
		return true;
	}

	private static char Peek( string source, int index ) => index < source.Length ? source[ index ] : '\0';

	/// <summary>The value of a text token with its quotes removed and \" \\ \n escapes applied.</summary>
	internal static string Unquote( string token )
	{
		var body = token.Length >= 2 && token[ ^1 ] == '"' ? token[ 1..^1 ] : token[ 1.. ];
		return body.Replace( "\\n", "\n" ).Replace( "\\\"", "\"" ).Replace( "\\\\", "\\" );
	}
}
