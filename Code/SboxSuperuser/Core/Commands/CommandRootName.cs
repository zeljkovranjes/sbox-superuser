namespace SboxSuperuser.Core.Commands;

internal static class CommandRootName
{
	internal static string Validate( string name )
	{
		if ( string.IsNullOrEmpty( name ) || name.Length > 32 || name[ 0 ] is not (>= 'a' and <= 'z')
			|| name.Any( value => value is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '_') ) )
		{
			throw new ArgumentException( "Root names require 1–32 lowercase letters, digits or underscores, starting with a letter.", nameof( name ) );
		}

		if ( name is "superuser" or "superuser_request" )
		{
			throw new ArgumentException( "This name is reserved for the fixed native transport.", nameof( name ) );
		}

		return name;
	}
}
