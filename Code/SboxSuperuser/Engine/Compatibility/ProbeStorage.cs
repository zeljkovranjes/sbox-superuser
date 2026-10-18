using Sandbox;

namespace SboxSuperuser.Engine.Compatibility;

internal static class ProbeStorage
{
	internal static void Run()
	{
		var mode = Application.IsDedicatedServer ? "dedicated" : "editor";
		var path = $"superuser-t01/{mode}/roundtrip.txt";
		try
		{
			Log.Info( $"[Superuser T01] storage path={FileSystem.Data.GetFullPath( path )}" );
			if ( FileSystem.Data.FileExists( path ) )
			{
				var previous = FileSystem.Data.ReadAllText( path );
				if ( previous is null || !previous.StartsWith( "T01:" ) || previous.Length > 128 )
				{
					Log.Warning( "[Superuser T01] storage: FAIL; unexpected existing probe file preserved." );
					return;
				}

				Log.Info( $"[Superuser T01] storage previous={previous}; compare with the previous session's token." );
			}
			else
			{
				Log.Info( "[Superuser T01] storage: first run; no previous probe file." );
			}

			FileSystem.Data.CreateDirectory( $"superuser-t01/{mode}" );
			var token = $"T01:{Guid.NewGuid():N}";
			FileSystem.Data.WriteAllTextAtomic( path, token );
			var matches = FileSystem.Data.ReadAllText( path ) == token;
			Log.Info( $"[Superuser T01] storage readback: {(matches ? "PASS" : "FAIL")}; token={token}; crash durability unverified" );
		}
		catch ( Exception exception )
		{
			Log.Warning( $"[Superuser T01] storage: FAIL; {exception}" );
		}
	}
}
