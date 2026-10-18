// Trusted development script: run only in the isolated compatibility lab.
if ( !Game.IsEditor || !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Isolated editor host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalSwitch = bootstrap.AllowEditorTesting;
const string owners = "superuser/editor-owners.json";
var originalOwners = FileSystem.Data.FileExists( owners ) ? FileSystem.Data.ReadAllText( owners ) : null;
var probe = TypeLibrary.GetType( "SuperuserCompatibilityLab.Engine.Commands.DispatchProbe" ).Create<SuperuserCompatibilityLab.IDispatchProbe>();
SboxSuperuser.CommandRegistrationScope registration = null;
object files = null;
System.Reflection.PropertyInfo checkpoint = null;
Task<SboxSuperuser.CommandResult> Execute( string command = "audit.sample", Guid? id = null ) => SboxSuperuser.Superuser.ExecuteCommandAsync(
	Connection.Local, new SboxSuperuser.CommandRequest( command, new[] { "t10-secret-sentinel", "42" }, id ?? Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Chat );
try
{
	FileSystem.Data.WriteAllText( owners, "{\"schemaVersion\":1,\"owners\":[\"" + Connection.Local.SteamId + "\"]}" );
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = true;
	bootstrap.Enabled = true;
	var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
	var store = runtime.GetType().GetProperty( "Store", flags ).GetValue( runtime );
	var coordinator = store.GetType().GetField( "_coordinator", flags ).GetValue( store );
	files = coordinator.GetType().GetField( "_files", flags ).GetValue( coordinator );
	checkpoint = files.GetType().GetProperty( "WriteCheckpoint", flags );
	registration = SboxSuperuser.Superuser.BeginCommandRegistration( "test.audit" );
	var arguments = new[]
	{
		new SboxSuperuser.CommandArgumentDescriptor( "text", "Text", SboxSuperuser.CommandArgumentKind.String ),
		new SboxSuperuser.CommandArgumentDescriptor( "count", "Count", SboxSuperuser.CommandArgumentKind.Integer, audit: SboxSuperuser.CommandArgumentAudit.SafeValue )
	};
	registration.RegisterCommand( new SboxSuperuser.CommandDescriptor( "audit.sample", "Audit fixture", "admin.help", "test.audit", arguments ),
		( _, _ ) => { probe.Increment(); return Task.FromResult( new SboxSuperuser.CommandResult( SboxSuperuser.CommandStatus.Success, "t10-secret-result" ) ); } );
	registration.RegisterCommand( new SboxSuperuser.CommandDescriptor( "audit.sensitive", "Sensitive fixture", "admin.help", "test.audit", new[]
	{
		new SboxSuperuser.CommandArgumentDescriptor( "password", "Password", SboxSuperuser.CommandArgumentKind.String, audit: SboxSuperuser.CommandArgumentAudit.Sensitive ), arguments[ 1 ]
	} ), ( _, _ ) => throw new InvalidOperationException( "Sensitive chat handler must never run." ) );
	registration.Commit();
	var id = Guid.NewGuid();
	Require( (await Execute( id: id )).Status == SboxSuperuser.CommandStatus.Success, "Audited mutation failed." );
	Require( (await Execute( id: id )).Status == SboxSuperuser.CommandStatus.Success && probe.Count == 1, "Retry repeated mutation." );
	var page = await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( requestId: id ) );
	var entry = page.Entries.Single();
	Require( entry.Outcome == SboxSuperuser.CommandStatus.Success && entry.ActorSubject == Connection.Local.SteamId.ToString()
		&& entry.SafeArguments[ "text" ] == "[redacted]" && entry.SafeArguments[ "count" ] == "42", "Attribution or redaction failed." );
	Require( (await Execute( "audit.sensitive" )).Status == SboxSuperuser.CommandStatus.Denied, "Sensitive chat command accepted." );

	checkpoint.SetValue( files, (Func<string, CancellationToken, Task>)(( path, _ ) => path.Contains( "/audit/journal/g-" )
		? Task.FromException( new System.IO.IOException( "Injected intent failure" ) ) : Task.CompletedTask) );
	Require( (await Execute()).Status == SboxSuperuser.CommandStatus.StorageUnavailable && probe.Count == 1, "Intent failure allowed mutation." );
	checkpoint.SetValue( files, (Func<string, CancellationToken, Task>)(( path, _ ) => path.Contains( "/audit/journal/g-" ) && probe.Count > 1
		? Task.FromException( new System.IO.IOException( "Injected outcome failure" ) ) : Task.CompletedTask) );
	var interrupted = Guid.NewGuid();
	Require( (await Execute( id: interrupted )).Status == SboxSuperuser.CommandStatus.Unknown && probe.Count == 2, "Outcome failure falsely reported success." );
	checkpoint.SetValue( files, null );
	Require( SboxSuperuser.Superuser.GetAuditDiagnostics().IsDegraded, "Audit failure was hidden." );
	page = await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( requestId: interrupted ) );
	Require( page.Entries.Single().Outcome == SboxSuperuser.CommandStatus.Unknown && page.Entries.Single().CompletedUtc is null, "Interrupted intent was lost." );
	Require( (await Execute()).Status == SboxSuperuser.CommandStatus.Success, "Healthy storage did not resume." );
	Require( (await SboxSuperuser.Superuser.ConfigureAuditRetentionAsync( Connection.Local, new SboxSuperuser.AuditRetentionOptions( 2 ) )).Status == SboxSuperuser.CommandStatus.Success,
		"Owner could not configure retention." );
	page = await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( pageSize: 1 ) );
	Require( page.NextCursor is not null, "Bounded page has no cursor." );
	Require( (await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( pageSize: 1, cursor: page.NextCursor ) )).Entries.Count == 1, "Second page missing." );
	await Execute();
	Require( (await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( cursor: page.NextCursor ) )).Status == SboxSuperuser.CommandStatus.Conflict,
		"Stale cursor accepted." );
	var auditFiles = FileSystem.Data.FindFile( "superuser/store/editor/audit/journal", "*", false ).ToArray();
	Require( auditFiles.Length == 4, "Retention left excess generations or orphans." );
	foreach ( var name in auditFiles )
	{
		var text = FileSystem.Data.ReadAllText( "superuser/store/editor/audit/journal/" + name.Split( '/' ).Last() );
		Require( !text.Contains( "t10-secret" ), "Secret leaked to audit disk files." );
	}
	Require( !SboxSuperuser.Superuser.GetAuditDiagnostics().IsDegraded, "Healthy retention remained degraded." );
	Log.Info( "[T10] editor audit PASS: attribution, dedup, redaction, sensitive chat refusal, real IO failures, pagination and physical retention" );
	return new { Pass = true, Mutations = probe.Count, AuditFiles = auditFiles.Length, InterruptedRequest = interrupted };
}
finally
{
	checkpoint?.SetValue( files, null );
	registration?.Dispose();
	if ( originalOwners is null ) FileSystem.Data.DeleteFile( owners );
	else FileSystem.Data.WriteAllText( owners, originalOwners );
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = true;
}
