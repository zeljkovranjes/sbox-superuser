using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SboxSuperuser.Core.Commands;

internal static class RequestFingerprint
{
	internal static string Compute( string command, IReadOnlyList<string> arguments )
	{
		var json = JsonSerializer.Serialize( new { command, arguments } );
		return Convert.ToHexString( SHA256.HashData( Encoding.UTF8.GetBytes( json ) ) );
	}
}
