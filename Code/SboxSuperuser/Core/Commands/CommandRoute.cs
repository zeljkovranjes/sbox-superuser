using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser.Core.Commands;

internal sealed record CommandRoute( string Command, IReadOnlyList<string> Arguments )
{
	internal static CommandRoute Resolve( IReadOnlyList<string> tokens )
	{
		var error = CommandTokenizer.ValidateTokens( tokens );
		if ( error is not null )
		{
			throw new ArgumentException( error, nameof( tokens ) );
		}

		if ( tokens.Count == 0 )
		{
			return new CommandRoute( "help", Array.Empty<string>() );
		}

		var command = tokens[ 0 ];
		var arguments = tokens.Skip( 1 ).ToArray();
		if ( command is "user" or "group" && tokens.Count >= 4
			&& tokens[ 2 ] is "group" or "permission" or "parents" or "protection" )
		{
			command += "." + tokens[ 2 ] + "." + tokens[ 3 ];
			arguments = new[] { tokens[ 1 ] }.Concat( tokens.Skip( 4 ) ).ToArray();
		}
		else if ( command == "settings" && tokens.Count >= 3 && tokens[ 1 ] is "get" or "set" or "reset" )
		{
			command += "." + tokens[ 1 ] + "." + tokens[ 2 ];
			arguments = tokens.Skip( 3 ).ToArray();
		}
		else if ( tokens.Count >= 2 && command is "group" or "permissions" or "audit" or "modules" or "settings" )
		{
			command += "." + tokens[ 1 ];
			arguments = tokens.Skip( 2 ).ToArray();
		}

		ContractGuard.Key( command, nameof( tokens ) );
		if ( command.Length > 96 )
		{
			throw new ArgumentException( "Command key exceeds 96 characters.", nameof( tokens ) );
		}

		return new CommandRoute( command, Array.AsReadOnly( arguments ) );
	}
}
