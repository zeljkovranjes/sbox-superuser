using System.Globalization;
using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Engine.NativeSettings;

internal static class NativeValue
{
	internal static string Key( string value )
	{
		ContractGuard.Key( value, nameof( value ) );
		if ( value.Length > 64 ) throw new ArgumentException( "Binding key exceeds 64 characters." );
		return value;
	}

	internal static string Name( string value )
	{
		if ( string.IsNullOrEmpty( value ) || value.Length > 96
			|| value.Any( character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_') ) )
			throw new ArgumentException( "Native name must contain only lowercase letters, digits and underscores." );
		return value;
	}

	internal static string Normalize( NativeSettingDescriptor binding, string value )
	{
		if ( value is null ) return null;
		var descriptor = new CommandDescriptor( "settings.value", binding.Description, binding.Permission,
			"superuser.settings", new[] { binding.Value } );
		var parsed = CommandArgumentParser.Parse( descriptor, new[] { value }, Array.Empty<CommandPlayer>() );
		return parsed.IsSuccess ? Format( binding.Value, parsed.Arguments ) : null;
	}

	internal static string Format( CommandArgumentDescriptor argument, CommandArguments values ) => argument.Kind switch
	{
		CommandArgumentKind.Integer => values.Get<long>( argument.Name ).ToString( CultureInfo.InvariantCulture ),
		CommandArgumentKind.Number => values.Get<double>( argument.Name ).ToString( "R", CultureInfo.InvariantCulture ),
		CommandArgumentKind.Boolean => values.Get<bool>( argument.Name ) ? "1" : "0",
		_ => values.Get<string>( argument.Name )
	};
}
