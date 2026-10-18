namespace SboxSuperuser.Core.Commands;

internal sealed class ChatCommandPrefix
{
	internal string Value { get; }

	internal ChatCommandPrefix( string commandRoot ) => Value = "/" + CommandRootName.Validate( commandRoot );

	internal bool Owns( string message ) => message is not null && message.StartsWith( Value, StringComparison.Ordinal )
		&& (message.Length == Value.Length || char.IsWhiteSpace( message[ Value.Length ] ));

	internal Tokenization Parse( string message )
	{
		if ( !Owns( message ) || message.Length > CommandTokenizer.MaximumTextLength )
		{
			return new Tokenization( Array.Empty<string>(), "Chat command is missing, unowned or oversized." );
		}
		return CommandTokenizer.Tokenize( message.Substring( Value.Length ) );
	}
}
