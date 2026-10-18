// Trusted development script; isolated dedicated fixture only. Restores existing configuration.
if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Dedicated fixture required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
const string path = "superuser/owners.json";
var existed = FileSystem.Data.FileExists( path );
var original = existed ? FileSystem.Data.ReadAllText( path ) : null;

void Restart()
{
	bootstrap.Enabled = false;
	bootstrap.Enabled = true;
}

try
{
	FileSystem.Data.CreateDirectory( "superuser" );
	FileSystem.Data.WriteAllText( path, "{\"schemaVersion\":1,\"owners\":[\"76561199542962865\"]}" );
	Restart();
	var valid = SboxSuperuser.Superuser.GetDiagnostics();
	if ( valid.State != SboxSuperuser.BootstrapState.Ready || valid.OwnerCount != 1
		|| valid.OwnerConfigurationPath != path || SboxSuperuser.Superuser.IsOwner( Connection.Local ) )
	{
		throw new InvalidOperationException( "Dedicated configuration or host-connection separation failed." );
	}

	FileSystem.Data.WriteAllText( path, "corrupt-test-input" );
	Restart();
	var invalid = SboxSuperuser.Superuser.GetDiagnostics();
	if ( invalid.State != SboxSuperuser.BootstrapState.OwnersInvalid || invalid.OwnerCount != 0
		|| FileSystem.Data.ReadAllText( path ) != "corrupt-test-input" )
	{
		throw new InvalidOperationException( "Corruption was accepted or overwritten." );
	}

	ConsoleSystem.Run( "superuser_local_identity" );
	return "PASS: dedicated owner file loads explicitly; corrupt file preserved without owners; local console remains distinct.";
}
finally
{
	if ( existed ) FileSystem.Data.WriteAllText( path, original );
	else FileSystem.Data.DeleteFile( path );
	Restart();
}
