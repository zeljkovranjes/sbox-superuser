using Sandbox;
using System.Text.Json;

namespace SboxSuperuser.Engine.Compatibility;

internal static class ProbeConsole
{
	internal static void Observe( Connection caller, string[] arguments )
	{
		if ( arguments.Length > 8 || arguments.Any( argument => argument.Length > 128 ) )
		{
			Log.Warning( "[Superuser T01] Use at most eight test arguments of 128 characters each." );
			return;
		}

		var identity = caller is null
			? "null (unclassified; no authority granted)"
			: $"connection={caller.Id}; steamId={caller.SteamId}; isHost={caller.IsHost}";
		Log.Info( $"[Superuser T01] caller={identity}; arguments={JsonSerializer.Serialize( arguments )}" );
		if ( caller is not null )
		{
			Log.Info( $"[Superuser T01] native claim sboxsuperuser.probe={caller.HasPermission( "sboxsuperuser.probe" )}; no role mapping" );
		}
	}
}
