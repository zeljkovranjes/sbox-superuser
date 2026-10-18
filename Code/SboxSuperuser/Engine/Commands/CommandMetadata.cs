namespace SboxSuperuser.Engine.Commands;

internal static class CommandMetadata
{
	internal static void Validate( CommandDescriptor command )
	{
		ArgumentNullException.ThrowIfNull( command );
		if ( command.Name.Length > 96 || command.ModuleId.Length > 96 || command.Permission.Length > 96
			|| command.Description.Length > 2048 || command.Arguments.Count > 32 || command.Aliases.Count > 16 )
		{
			throw new ArgumentException( "Command metadata exceeds its size limits." );
		}

		var names = new HashSet<string>( StringComparer.Ordinal ) { command.Name };
		foreach ( var alias in command.Aliases )
		{
			if ( alias.Length > 96 || !names.Add( alias ) )
			{
				throw new ArgumentException( $"Duplicate or oversized alias '{alias}'." );
			}
		}

		names.Clear();
		var optionalSeen = false;
		for ( var index = 0; index < command.Arguments.Count; index++ )
		{
			var argument = command.Arguments[ index ];
			if ( argument.Name.Length > 96 || argument.Description.Length > 2048 || !names.Add( argument.Name ) )
			{
				throw new ArgumentException( "Argument names must be unique and metadata bounded." );
			}

			if ( optionalSeen && !argument.IsOptional )
			{
				throw new ArgumentException( "Required arguments cannot follow optional arguments." );
			}

			optionalSeen |= argument.IsOptional;
			if ( argument.Audit == CommandArgumentAudit.SafeValue && argument.Kind == CommandArgumentKind.String )
			{
				throw new ArgumentException( "Free-text arguments must be redacted; safe audit values require a non-text type." );
			}

			if ( argument.IsRemainder && (argument.Kind != CommandArgumentKind.String || index != command.Arguments.Count - 1) )
			{
				throw new ArgumentException( "Only the final string argument may consume the remainder." );
			}

			if ( (argument.Minimum.HasValue || argument.Maximum.HasValue)
				&& argument.Kind is not (CommandArgumentKind.Integer or CommandArgumentKind.Number) )
			{
				throw new ArgumentException( "Numeric bounds require a numeric argument." );
			}

			if ( (argument.Kind == CommandArgumentKind.Player) != (argument.TargetPolicy != CommandTargetPolicy.None) )
			{
				throw new ArgumentException( "Player arguments require an explicit target policy; other arguments cannot have one." );
			}

			var choices = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
			foreach ( var choice in argument.Choices )
			{
				if ( string.IsNullOrWhiteSpace( choice ) || choice.Length > 96 || choice.Any( char.IsWhiteSpace ) || !choices.Add( choice ) )
				{
					throw new ArgumentException( "Enum choices must be unique, nonempty single tokens." );
				}
			}

			if ( choices.Count > 256 || (argument.Kind == CommandArgumentKind.Enum) != (choices.Count > 0) )
			{
				throw new ArgumentException( "Only enum arguments declare choices, with between 1 and 256 choices." );
			}
		}
	}
}
