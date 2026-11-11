using System.Text.RegularExpressions;

namespace SboxSuperuser.Core.Alts;

/// <summary>
/// Recognises the text of an IP address without System.Net, which s&amp;box's code whitelist doesn't allow. Ports are
/// dropped and loopback addresses ignored, so the same network gives the same text.
/// </summary>
internal static class IpAddress
{
	private static readonly Regex Ipv4 = new( @"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})(:\d+)?$", RegexOptions.CultureInvariant );
	private static readonly Regex Ipv6 = new( @"^\[?([0-9a-fA-F:]{2,39})\]?(:\d+)?$", RegexOptions.CultureInvariant );

	/// <summary>The address without its port, lowercase; null for anything else, such as a Steam ID or loopback.</summary>
	internal static string Normalize( string address )
	{
		if ( string.IsNullOrWhiteSpace( address ) ) return null;
		if ( Ipv4.Match( address.Trim() ) is { Success: true } v4 )
		{
			var parts = Enumerable.Range( 1, 4 ).Select( group => int.Parse( v4.Groups[ group ].Value ) ).ToArray();
			if ( parts.Any( part => part > 255 ) || parts[ 0 ] == 127 ) return null;
			return string.Join( '.', parts );
		}
		if ( Ipv6.Match( address.Trim() ) is { Success: true } v6 && v6.Groups[ 1 ].Value.Contains( ':' ) )
		{
			var ip = v6.Groups[ 1 ].Value.ToLowerInvariant();
			return ip is "::1" ? null : ip;
		}
		return null;
	}
}
