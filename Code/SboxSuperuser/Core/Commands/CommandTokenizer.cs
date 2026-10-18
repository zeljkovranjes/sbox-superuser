using System.Text;

namespace SboxSuperuser.Core.Commands;

internal static class CommandTokenizer
{
	internal const int MaximumTextLength = 4096;
	internal const int MaximumTokenLength = 512;
	internal const int MaximumTokens = 64;

	internal static Tokenization Tokenize( string text )
	{
		if ( text is null || text.Length > MaximumTextLength || text.Any( value => char.IsControl( value ) && value != '\t' ) )
		{
			return Failed( "Command text is missing, oversized or contains control characters." );
		}

		var tokens = new List<string>();
		var index = 0;
		while ( index < text.Length )
		{
			if ( char.IsWhiteSpace( text[ index ] ) )
			{
				index++;
				continue;
			}

			if ( tokens.Count >= MaximumTokens )
			{
				return Failed( "Too many command tokens." );
			}

			var quote = text[ index ] is '\'' or '"' ? text[ index++ ] : '\0';
			var token = new StringBuilder();
			var closed = quote == '\0';
			while ( index < text.Length )
			{
				var character = text[ index++ ];
				if ( quote != '\0' && character == quote )
				{
					closed = true;
					if ( index < text.Length && !char.IsWhiteSpace( text[ index ] ) )
					{
						return Failed( "A closing quote must be followed by whitespace or the end of input." );
					}

					break;
				}

				if ( quote == '\0' && char.IsWhiteSpace( character ) )
				{
					break;
				}

				if ( quote == '\0' && character is '\'' or '"' )
				{
					return Failed( "Quotes must surround a complete token." );
				}

				if ( quote != '\0' && character == '\\' )
				{
					if ( index >= text.Length || text[ index ] != '\\' && text[ index ] != quote )
					{
						return Failed( "Inside quotes, escape only the matching quote or a backslash." );
					}

					character = text[ index++ ];
				}

				token.Append( character );
				if ( token.Length > MaximumTokenLength )
				{
					return Failed( "A command token is too long." );
				}
			}

			if ( !closed )
			{
				return Failed( "Unterminated quoted token." );
			}

			tokens.Add( token.ToString() );
		}

		return new Tokenization( tokens.AsReadOnly(), null );
	}

	internal static string ValidateTokens( IReadOnlyList<string> tokens )
	{
		if ( tokens is null || tokens.Count > MaximumTokens )
		{
			return "Command tokens are missing or exceed the count limit.";
		}

		var length = 0;
		foreach ( var token in tokens )
		{
			if ( token is null || token.Length > MaximumTokenLength || token.Any( char.IsControl ) )
			{
				return "A command token is missing, oversized or contains control characters.";
			}

			length += token.Length + 1;
			if ( length > MaximumTextLength )
			{
				return "Command tokens exceed the total input limit.";
			}
		}

		return null;
	}

	private static Tokenization Failed( string error ) => new( Array.Empty<string>(), error );
}
