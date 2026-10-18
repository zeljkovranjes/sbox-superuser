// Trusted development script. Use only the isolated editor-host fixture.
if ( !Game.IsEditor || !Networking.IsHost || !Networking.IsActive )
{
	throw new InvalidOperationException( "Start the isolated editor-host fixture." );
}

var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
const string path = "superuser/editor-owners.json";
var existed = FileSystem.Data.FileExists( path );
var original = existed ? FileSystem.Data.ReadAllText( path ) : null;
var originalOptIn = bootstrap.AllowEditorTesting;
GameObject duplicate = null;

void Require( bool condition, string message )
{
	if ( !condition ) throw new InvalidOperationException( message );
}

void Restart()
{
	bootstrap.Enabled = false;
	bootstrap.Enabled = true;
}

try
{
	bootstrap.AllowEditorTesting = true;
	FileSystem.Data.CreateDirectory( "superuser" );
	FileSystem.Data.WriteAllText( path, "{\"schemaVersion\":1,\"owners\":[]}" );
	Restart();
	Require( SboxSuperuser.Superuser.GetDiagnostics().State == SboxSuperuser.BootstrapState.Ready, "Valid empty configuration failed." );
	Require( !SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Host became an automatic owner." );
	Require( !SboxSuperuser.Superuser.TryGetPlayerIdentity( null, out _ ) && !SboxSuperuser.Superuser.IsOwner( null ), "Null gained authority." );
	Require( SboxSuperuser.Superuser.TryGetPlayerIdentity( Connection.Local, out var actor )
		&& actor.Kind == SboxSuperuser.ActorKind.Player && actor.SubjectId == Connection.Local.SteamId.ToString(), "Player identity incorrect." );

	var runtime = SboxSuperuser.Superuser.GetDiagnostics().RuntimeId;
	typeof(SboxSuperuser.Components.SuperuserBootstrap).GetMethod( "OnStart",
		System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic ).Invoke( bootstrap, null );
	Require( runtime == SboxSuperuser.Superuser.GetDiagnostics().RuntimeId, "Repeated initialization replaced runtime." );

	FileSystem.Data.WriteAllText( path, $"{{\"schemaVersion\":1,\"owners\":[\"{actor.SubjectId}\"]}}" );
	Restart();
	Require( SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Explicit owner was not loaded." );
	duplicate = new GameObject( Game.ActiveScene );
	duplicate.Components.Create<SboxSuperuser.Components.SuperuserBootstrap>().AllowEditorTesting = true;
	Require( SboxSuperuser.Superuser.GetDiagnostics().State == SboxSuperuser.BootstrapState.DuplicateBootstrap
		&& SboxSuperuser.Superuser.GetDiagnostics().RuntimeId is null
		&& !SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Duplicate bootstrap retained authority." );
	duplicate.Destroy();
	duplicate = null;
	await Task.Delay( 100, cancellationToken );
	Require( SboxSuperuser.Superuser.GetDiagnostics().BootstrapCount == 1
		&& SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Duplicate removal did not recover one runtime." );

	const string invalid = "{\"schemaVersion\":1,\"owners\":[\"0\"]}";
	FileSystem.Data.WriteAllText( path, invalid );
	Restart();
	Require( SboxSuperuser.Superuser.GetDiagnostics().State == SboxSuperuser.BootstrapState.OwnersInvalid
		&& !SboxSuperuser.Superuser.IsOwner( Connection.Local )
		&& FileSystem.Data.ReadAllText( path ) == invalid, "Invalid owner file was accepted or overwritten." );

	bootstrap.AllowEditorTesting = false;
	Restart();
	Require( SboxSuperuser.Superuser.GetDiagnostics().RuntimeId is null, "Editor default-off retained a runtime." );
	bootstrap.AllowEditorTesting = true;
	Restart();
	ConsoleSystem.Run( "superuser_identity", "DedicatedConsole", "owner=true" );
	ConsoleSystem.Run( "superuser_local_identity" );
	Log.Info( "[Superuser T03] PASS: null refusal, exact identity, explicit owners, malformed config, duplicate suspension, repeated initialization and editor opt-in." );
}
finally
{
	duplicate?.Destroy();
	if ( existed ) FileSystem.Data.WriteAllText( path, original );
	else FileSystem.Data.DeleteFile( path );
	bootstrap.AllowEditorTesting = originalOptIn;
	Restart();
}

return "Bootstrap verification passed; original owner file and editor option restored.";
