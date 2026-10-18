namespace SboxSuperuser.Core.Identity;

internal static class SteamIdentity
{
	internal static string Validate( string value, string parameter )
	{
		if ( string.IsNullOrEmpty( value ) || value[ 0 ] == '0'
			|| value.Any( character => character is < '0' or > '9' )
			|| !ulong.TryParse( value, out _ ) )
		{
			throw new ArgumentException( "Use a nonzero canonical decimal Steam ID.", parameter );
		}

		return value;
	}
}
