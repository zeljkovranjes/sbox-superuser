using Sandbox;

namespace SboxSuperuser.Engine.Compatibility;

internal static class ProbeStorage
{
	internal static void Run()
	{
		var mode = Application.IsDedicatedServer ? "dedicated" : "editor";
		var directory = $"superuser-t01/{mode}";
		var path = $"{directory}/baseline.txt";
		var candidatePath = $"{directory}/candidate.txt";
		try
		{
			Log.Info( $"[Superuser T01] storage path={FileSystem.Data.GetFullPath( path )}" );
			string token;
			if ( FileSystem.Data.FileExists( path ) )
			{
				var previous = FileSystem.Data.ReadAllText( path );
				if ( previous is null || !previous.StartsWith( "T01:" )
					|| !Guid.TryParseExact( previous.Substring( 4 ), "N", out _ ) )
				{
					Log.Warning( "[Superuser T01] storage: FAIL; unexpected existing probe file preserved." );
					return;
				}

				Log.Info( $"[Superuser T01] storage previous={previous}; compare with the previous session's token." );
				token = previous;
			}
			else
			{
				Log.Info( "[Superuser T01] storage: first run; no previous probe file." );
				FileSystem.Data.CreateDirectory( directory );
				token = $"T01:{Guid.NewGuid():N}";
				FileSystem.Data.WriteAllText( path, token );
			}

			var matches = FileSystem.Data.ReadAllText( path ) == token;
			Log.Info( $"[Superuser T01] storage readback: {(matches ? "PASS" : "FAIL")}; token={token}; crash durability unverified" );
			if ( !matches )
			{
				return;
			}

			FileSystem.Data.WriteAllText( candidatePath, token );
			var candidateMatches = FileSystem.Data.ReadAllText( candidatePath ) == token;
			// Simulate an incomplete candidate without overwriting the previous readable file.
			FileSystem.Data.WriteAllText( candidatePath, "T01:" );
			var preserved = FileSystem.Data.ReadAllText( path ) == token;
			Log.Info( $"[Superuser T01] separate candidate: {(candidateMatches && preserved ? "PASS" : "FAIL")}; simulated truncation, not a process crash" );
		}
		catch ( Exception exception )
		{
			Log.Warning( $"[Superuser T01] storage: FAIL; {exception}" );
		}
	}
}
