using System.Globalization;

namespace SboxSuperuser.Engine.Commands;

internal static class CommandSyntaxBuilder
{
	internal static CommandSyntax Build( CommandDescriptor descriptor )
	{
		CommandMetadata.Validate( descriptor );
		var tokens = new List<string> { descriptor.Name };
		var help = new List<string>();
		foreach ( var argument in descriptor.Arguments )
		{
			var label = argument.Name + (argument.IsRemainder ? "..." : "");
			tokens.Add( argument.IsOptional ? $"[{label}]" : $"<{label}>" );
			var constraints = new List<string> { argument.Kind.ToString().ToLowerInvariant() };
			if ( argument.Minimum.HasValue )
			{
				constraints.Add( "minimum " + argument.Minimum.Value.ToString( "R", CultureInfo.InvariantCulture ) );
			}

			if ( argument.Maximum.HasValue )
			{
				constraints.Add( "maximum " + argument.Maximum.Value.ToString( "R", CultureInfo.InvariantCulture ) );
			}

			if ( argument.Kind == CommandArgumentKind.Enum )
			{
				constraints.Add( "choices: " + string.Join( ", ", argument.Choices.Take( 16 ) ) + (argument.Choices.Count > 16 ? ", …" : "") );
			}

			if ( argument.Kind == CommandArgumentKind.Duration )
			{
				constraints.Add( "positive integer + s/m/h/d/w, or permanent" );
			}

			if ( argument.Kind == CommandArgumentKind.Boolean )
			{
				constraints.Add( "true or false" );
			}

			if ( argument.Kind == CommandArgumentKind.Player )
			{
				constraints.Add( "exact Steam ID or unique online display name" );
				constraints.Add( argument.TargetPolicy.ToString() );
			}

			help.Add( $"{argument.Name}: {argument.Description} ({string.Join( "; ", constraints )})" );
		}

		return new CommandSyntax( string.Join( " ", tokens ), descriptor.Description, descriptor.Permission, help );
	}
}
