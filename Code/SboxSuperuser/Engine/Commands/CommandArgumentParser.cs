using SboxSuperuser.Core.Commands;

namespace SboxSuperuser.Engine.Commands;

internal static class CommandArgumentParser
{
	internal static CommandParseResult Parse( CommandDescriptor descriptor, IReadOnlyList<string> tokens, IReadOnlyList<CommandPlayer> onlinePlayers )
	{
		CommandMetadata.Validate( descriptor );
		var tokenError = CommandTokenizer.ValidateTokens( tokens );
		if ( tokenError is not null )
		{
			return Failed( tokenError );
		}

		if ( onlinePlayers is null || onlinePlayers.Count > 1024 || onlinePlayers.Any( value => value is null ) )
		{
			return Failed( "The online player directory is unavailable or exceeds its limit." );
		}

		var players = onlinePlayers.Select( value => value.Candidate ).ToArray();
		var values = new Dictionary<string, object>( StringComparer.Ordinal );
		var index = 0;
		foreach ( var argument in descriptor.Arguments )
		{
			if ( index == tokens.Count )
			{
				return argument.IsOptional ? new CommandParseResult( new CommandArguments( values ), null )
					: Failed( $"Missing required argument '{argument.Name}'." );
			}

			var text = argument.IsRemainder ? string.Join( " ", tokens.Skip( index ) ) : tokens[ index ];
			index = argument.IsRemainder ? tokens.Count : index + 1;
			if ( argument.Kind == CommandArgumentKind.Player )
			{
				var requiresOnline = argument.TargetPolicy is CommandTargetPolicy.OnlinePlayer or CommandTargetPolicy.PunitiveOnlinePlayer;
				var result = PlayerSelection.Resolve( text, players, requiresOnline );
				if ( result.Error is not null )
				{
					return Failed( $"Argument '{argument.Name}': {result.Error}", result.Choices.Select( player => new CommandPlayer( player.SteamId, player.DisplayName ) ) );
				}

				values.Add( argument.Name, new PlayerReference( result.SteamId ) );
				continue;
			}

			var value = Convert( argument, text );
			if ( value is null )
			{
				return Failed( $"Argument '{argument.Name}' must satisfy its {argument.Kind.ToString().ToLowerInvariant()} type and declared constraints." );
			}

			values.Add( argument.Name, value );
		}

		return index != tokens.Count ? Failed( "Too many arguments for this command." )
			: new CommandParseResult( new CommandArguments( values ), null );
	}

	private static object Convert( CommandArgumentDescriptor argument, string text )
	{
		switch ( argument.Kind )
		{
			case CommandArgumentKind.String:
				return text;
			case CommandArgumentKind.Integer:
				return ScalarConversion.Integer( text, argument.Minimum, argument.Maximum, out var integer ) ? integer : null;
			case CommandArgumentKind.Number:
				return ScalarConversion.Number( text, argument.Minimum, argument.Maximum, out var number ) ? number : null;
			case CommandArgumentKind.Boolean:
				return ScalarConversion.Boolean( text, out var boolean ) ? boolean : null;
			case CommandArgumentKind.Enum:
				return argument.Choices.FirstOrDefault( choice => string.Equals( choice, text, StringComparison.OrdinalIgnoreCase ) );
			case CommandArgumentKind.Duration:
				if ( !ScalarConversion.Duration( text, out var duration ) )
				{
					return null;
				}

				return duration.HasValue ? new CommandDuration( duration.Value ) : CommandDuration.Permanent;
			default:
				throw new ArgumentException( "Unsupported argument kind." );
		}
	}

	private static CommandParseResult Failed( string error, IEnumerable<CommandPlayer> choices = null ) => new( null, error, choices );
}
