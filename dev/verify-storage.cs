// Trusted development method body, outside the shipped library and API whitelist.
if ( !Application.IsDedicatedServer || !Networking.IsHost )
{
	throw new InvalidOperationException( "Use the isolated dedicated test server." );
}

const string path = "superuser-t01/dedicated/baseline.txt";
var original = FileSystem.Data.ReadAllText( path );
if ( original is null || !original.StartsWith( "T01:" )
	|| !Guid.TryParseExact( original.Substring( 4 ), "N", out _ ) )
{
	throw new InvalidOperationException( "First establish a valid test baseline." );
}

try
{
	FileSystem.Data.WriteAllText( path, "invalid-test-baseline" );
	var adapter = typeof(SboxSuperuser.Components.CompatibilityProbe).Assembly
		.GetType( "SboxSuperuser.Engine.Compatibility.ProbeStorage" );
	adapter.GetMethod( "Run", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic )
		.Invoke( null, null );
	if ( FileSystem.Data.ReadAllText( path ) != "invalid-test-baseline" )
	{
		throw new InvalidOperationException( "Probe overwrote the invalid baseline." );
	}

	return "PASS: corrupt baseline preserved; original restored in finally.";
}
finally
{
	FileSystem.Data.WriteAllText( path, original );
}
