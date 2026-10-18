using System.Globalization;

namespace SboxSuperuser.Engine.Audit;

internal static class AuditRedaction
{
	internal static IReadOnlyDictionary<string, string> Arguments( CommandDescriptor descriptor, CommandArguments arguments )
	{
		var values = new Dictionary<string, string>( StringComparer.Ordinal );
		foreach ( var argument in descriptor?.Arguments ?? Array.Empty<CommandArgumentDescriptor>() )
		{
			if ( arguments is not null && !arguments.Contains( argument.Name ) )
			{
				continue;
			}

			values[ argument.Name ] = argument.Audit == CommandArgumentAudit.SafeValue && !SensitiveName( argument.Name )
				&& arguments is not null ? SafeValue( argument, arguments ) : "[redacted]";
		}

		return values;
	}

	internal static IReadOnlyList<string> Targets( CommandDescriptor descriptor, CommandArguments arguments )
	{
		return arguments is null ? Array.Empty<string>() : descriptor.Arguments
			.Where( argument => argument.Kind == CommandArgumentKind.Player && arguments.Contains( argument.Name ) )
			.Select( argument => arguments.Get<PlayerReference>( argument.Name ).SteamId ).Distinct( StringComparer.Ordinal ).ToArray();
	}

	internal static bool SensitiveName( string name ) => name.Contains( "password", StringComparison.OrdinalIgnoreCase )
		|| name.Contains( "secret", StringComparison.OrdinalIgnoreCase ) || name.Contains( "token", StringComparison.OrdinalIgnoreCase )
		|| name.Contains( "credential", StringComparison.OrdinalIgnoreCase ) || name.Contains( "apikey", StringComparison.OrdinalIgnoreCase )
		|| name.Contains( "api_key", StringComparison.OrdinalIgnoreCase ) || name == "pin";

	private static string SafeValue( CommandArgumentDescriptor descriptor, CommandArguments arguments )
	{
		return descriptor.Kind switch
		{
			CommandArgumentKind.Integer => arguments.Get<long>( descriptor.Name ).ToString( CultureInfo.InvariantCulture ),
			CommandArgumentKind.Number => arguments.Get<double>( descriptor.Name ).ToString( "R", CultureInfo.InvariantCulture ),
			CommandArgumentKind.Boolean => arguments.Get<bool>( descriptor.Name ) ? "true" : "false",
			CommandArgumentKind.Enum => arguments.Get<string>( descriptor.Name ),
			CommandArgumentKind.Player => arguments.Get<PlayerReference>( descriptor.Name ).SteamId,
			CommandArgumentKind.Duration => Duration( arguments.Get<CommandDuration>( descriptor.Name ) ),
			_ => "[redacted]"
		};
	}

	private static string Duration( CommandDuration duration ) => duration.IsPermanent ? "permanent"
		: (duration.Value.Value.Ticks / TimeSpan.TicksPerSecond).ToString( CultureInfo.InvariantCulture ) + "s";
}
