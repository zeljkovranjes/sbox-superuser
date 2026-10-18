// Trusted development method body. Uses actual native entry points; run on the isolated editor or dedicated lab.
if ( !Networking.IsHost || !Networking.IsActive || !Application.IsDedicatedServer && !Game.IsEditor )
	throw new InvalidOperationException( "Eligible isolated host required." );
void Require( bool condition, string message ) { if ( !condition ) throw new InvalidOperationException( message ); }
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalSwitch = bootstrap.AllowEditorTesting;
var originalRoot = bootstrap.CommandRoot;
const string editorOwners = "superuser/editor-owners.json";
var originalOwners = FileSystem.Data.FileExists( editorOwners ) ? FileSystem.Data.ReadAllText( editorOwners ) : null;
void Restart( string root )
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = Game.IsEditor;
	bootstrap.CommandRoot = root;
	bootstrap.Enabled = true;
}
object Runtime() => typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
async Task<SboxSuperuser.AuditPage> Query( string command )
{
	var runtime = Runtime();
	var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
	SboxSuperuser.ActorIdentity Authenticate()
	{
		if ( !Application.IsDedicatedServer ) return SboxSuperuser.Superuser.TryGetPlayerIdentity( Connection.Local, out var player ) ? player : null;
		var parameters = new object[] { null };
		return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, parameters ) ? (SboxSuperuser.ActorIdentity)parameters[ 0 ] : null;
	}
	return await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
	{
		(Func<SboxSuperuser.ActorIdentity>)Authenticate, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
		(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( command: command ), CancellationToken.None
	} );
}
async Task<SboxSuperuser.AuditEntry> Run( string command, Action invoke )
{
	var previous = (await Query( command )).Entries.Select( entry => entry.Id ).ToHashSet();
	invoke();
	for ( var attempt = 0; attempt < 200; attempt++ )
	{
		var latest = (await Query( command )).Entries.FirstOrDefault( entry => !previous.Contains( entry.Id ) && entry.CompletedUtc.HasValue );
		if ( latest is not null ) return latest;
		await Task.Delay( 25, cancellationToken );
	}
	throw new InvalidOperationException( "Native request did not reach an audited outcome: " + command );
}
try
{
	if ( Game.IsEditor ) FileSystem.Data.WriteAllText( editorOwners, "{\"schemaVersion\":1,\"owners\":[\"" + Connection.Local.SteamId + "\"]}" );
	Restart( "admin" );
	Require( SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.ConsoleUnavailable, "Native metadata collision." );
	var help = await Run( "help", () => ConsoleSystem.Run( "admin", "help", "group.create" ) );
	Require( help.Outcome == SboxSuperuser.CommandStatus.Success, "Native help failed." );
	var group = "t11native" + Guid.NewGuid().ToString( "N" ).Substring( 0, 8 );
	var created = await Run( "group.create", () => ConsoleSystem.Run( "admin", "group", "create", group ) );
	Require( created.Outcome == SboxSuperuser.CommandStatus.Success, "Native access mutation failed." );
	Require( created.ActorKind == (Application.IsDedicatedServer ? SboxSuperuser.ActorKind.DedicatedConsole : SboxSuperuser.ActorKind.Player), "Caller kinds were conflated." );
	Require( (await Run( "group.delete", () => ConsoleSystem.Run( "admin", "group", "delete", group ) )).Outcome == SboxSuperuser.CommandStatus.Success, "Native delete failed." );
	Require( (await Run( "diagnostics", () => ConsoleSystem.Run( "admin diagnostics" ) )).Outcome == SboxSuperuser.CommandStatus.Success, "Native diagnostics failed." );
	Restart( "superuser_probe" );
	Require( SboxSuperuser.Superuser.GetDiagnostics().State == SboxSuperuser.BootstrapState.ConsoleUnavailable, "Existing managed command collision was not reported." );
	Restart( "operators" );
	Require( (await Run( "version", () => ConsoleSystem.Run( "superuser", "operators", "version" ) )).Outcome == SboxSuperuser.CommandStatus.Success,
		"Configured logical root failed through fixed transport." );
	Log.Info( "[T11] native console PASS: help, access, distinct actor, diagnostics, root collision and configured fallback" );
	return new { Pass = true, Dedicated = Application.IsDedicatedServer, created.ActorKind, created.ActorSubject, created.RequestId };
}
finally
{
	if ( Game.IsEditor )
	{
		if ( originalOwners is null ) FileSystem.Data.DeleteFile( editorOwners );
		else FileSystem.Data.WriteAllText( editorOwners, originalOwners );
	}
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.CommandRoot = originalRoot;
	bootstrap.Enabled = true;
}
