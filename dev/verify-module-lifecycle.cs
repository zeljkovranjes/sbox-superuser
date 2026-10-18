// Trusted development method body; run in the isolated editor or actual dedicated fixture.
if ( !Networking.IsHost || !Networking.IsActive || !Application.IsDedicatedServer && !Game.IsEditor )
	throw new InvalidOperationException( "Eligible isolated host required." );
void Require( bool condition, string message ) { if ( !condition ) throw new InvalidOperationException( message ); }
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() )
	.OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalSwitch = bootstrap.AllowEditorTesting;
const string ownersPath = "superuser/editor-owners.json";
var originalOwners = FileSystem.Data.FileExists( ownersPath ) ? FileSystem.Data.ReadAllText( ownersPath ) : null;
object Runtime() => typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
async Task<SboxSuperuser.AuditPage> Query( string command )
{
	var runtime = Runtime();
	var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
	SboxSuperuser.ActorIdentity Authenticate()
	{
		if ( !Application.IsDedicatedServer ) return SboxSuperuser.Superuser.TryGetPlayerIdentity( Connection.Local, out var actor ) ? actor : null;
		var parameters = new object[] { null };
		return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, parameters )
			? (SboxSuperuser.ActorIdentity)parameters[ 0 ] : null;
	}
	return await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
	{
		(Func<SboxSuperuser.ActorIdentity>)Authenticate, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
		(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( command: command ), CancellationToken.None
	} );
}
async Task<SboxSuperuser.CommandStatus> Run( string command, params string[] arguments )
{
	var auditKey = SboxSuperuser.Superuser.GetCommandDescriptors().Any( item => item.Name == command ) ? command : "command.unresolved";
	var previous = (await Query( auditKey )).Entries.Select( item => item.Id ).ToHashSet();
	ConsoleSystem.Run( "admin", new object[] { command }.Concat( arguments.Cast<object>() ).ToArray() );
	for ( var i = 0; i < 240; i++ )
	{
		var entry = (await Query( auditKey )).Entries.FirstOrDefault( item => !previous.Contains( item.Id ) && item.CompletedUtc.HasValue );
		if ( entry is not null ) return entry.Outcome;
		await Task.Delay( 25, cancellationToken );
	}
	throw new InvalidOperationException( "No audited native result: " + command );
}
var pulses = new SuperuserCompatibilityLab.Engine.Modules.SamplePulseSource();
var sample = new SuperuserCompatibilityLab.Engine.Modules.SampleCounterModule( pulses );
var held = new SuperuserCompatibilityLab.Engine.Modules.HeldCommandModule();
try
{
	if ( Game.IsEditor ) FileSystem.Data.WriteAllText( ownersPath, "{\"schemaVersion\":1,\"owners\":[\"" + Connection.Local.SteamId + "\"]}" );
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = Game.IsEditor;
	bootstrap.Enabled = true;
	Require( (await SboxSuperuser.Superuser.RegisterModulesAsync( new SboxSuperuser.IAdminModule[] { sample, held }, cancellationToken )).Status == SboxSuperuser.CommandStatus.Success, "Sample startup failed." );
	var store = SboxSuperuser.Superuser.GetStore();
	var key = new SboxSuperuser.StoreKey( "sample.counter", "t12-check" );
	var previous = await store.ReadAsync( key, 1, cancellationToken );
	var revision = previous.Document?.Revision ?? 0;
	var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.InternalService,
		"t12.fixture", SboxSuperuser.CommandOrigin.InternalService, "sample.seed", DateTimeOffset.UtcNow );
	var write = new SboxSuperuser.PolicyTransaction( "{\"retained\":true}", new[] { intent } ).CreateWriteRequest( key, revision );
	var document = write.Document;
	var written = await store.WriteAsync( write, cancellationToken );
	Require( written.Status == SboxSuperuser.StoreWriteStatus.Committed, "Sample data write failed: " + written.Status );
	for ( var iteration = 0; iteration < 5; iteration++ )
	{
		Require( pulses.Listeners == 1, "Duplicate or missing listener." );
		pulses.Raise();
		Require( sample.Pulses == iteration + 1, "Event executed more than once." );
		Require( await Run( "sample.count" ) == SboxSuperuser.CommandStatus.Success, "Sample command failed." );
		Require( sample.Executions == iteration + 1, "Command executed more than once." );
		Require( await Run( "modules.disable", "sample.counter" ) == SboxSuperuser.CommandStatus.Success, "Disable failed." );
		Require( pulses.Listeners == 0, "Listener leaked after disable." );
		pulses.Raise();
		Require( sample.Pulses == iteration + 1, "Disabled listener ran." );
		Require( await Run( "sample.count" ) == SboxSuperuser.CommandStatus.NotFound, "Disabled command remained registered." );
		Require( (await store.ReadAsync( key, 1, cancellationToken )).Document.PayloadJson == document.PayloadJson, "Disable deleted stored data." );
		Require( await Run( "modules.enable", "sample.counter" ) == SboxSuperuser.CommandStatus.Success, "Enable failed." );
	}
	Require( await Run( "modules.disable", "superuser.core" ) == SboxSuperuser.CommandStatus.Denied, "Required core disabled." );
	Require( await Run( "modules.disable", "superuser.administration" ) == SboxSuperuser.CommandStatus.Denied, "Required console disabled." );
	ConsoleSystem.Run( "admin", "sample.hold" );
	for ( var attempt = 0; !held.Entered && attempt < 100; attempt++ ) await Task.Delay( 20, cancellationToken );
	Require( held.Entered, "Held command did not enter." );
	var started = DateTimeOffset.UtcNow;
	Require( await Run( "modules.disable", "sample.held" ) == SboxSuperuser.CommandStatus.Unknown, "Uncooperative handler was incorrectly reported drained." );
	var drainSeconds = (DateTimeOffset.UtcNow - started).TotalSeconds;
	Require( drainSeconds < 8 && held.CancellationSeen && held.Stops == 0, "Disable failed to bound the wait or cleaned up before execution ended." );
	Require( await Run( "modules.enable", "sample.held" ) == SboxSuperuser.CommandStatus.Conflict, "Quarantined generation overlapped a restart." );
	held.Release();
	for ( var attempt = 0; held.Stops == 0 && attempt < 100; attempt++ ) await Task.Delay( 20, cancellationToken );
	Require( held.Stops == 1, "Released command did not finish cleanup." );
	Require( (await Query( "sample.hold" )).Entries.First().Outcome == SboxSuperuser.CommandStatus.Unknown, "Interrupted mutation was not audited Unknown." );
	var failure = new SuperuserCompatibilityLab.Engine.Modules.FailingStartupModule( pulses );
	Require( (await SboxSuperuser.Superuser.RegisterModulesAsync( new SboxSuperuser.IAdminModule[] { failure }, cancellationToken )).Status == SboxSuperuser.CommandStatus.Failed, "Injected startup unexpectedly passed." );
	Require( pulses.Listeners == 1, "Failed startup leaked a listener." );
	Require( await Run( "sample.failure" ) == SboxSuperuser.CommandStatus.NotFound, "Failed startup published a command." );
	Require( await Run( "modules.list" ) == SboxSuperuser.CommandStatus.Success, "Module list failed." );
	var states = SboxSuperuser.Superuser.GetModuleStatuses().Select( item => new { item.Descriptor.Id, item.State } ).ToArray();
	Log.Info( "[T12] PASS: native module management, five reloads, one listener and execution, data retention, required security and startup rollback." );
	return new { Pass = true, Dedicated = Application.IsDedicatedServer, sample.Executions, sample.Pulses, sample.Stops, drainSeconds, states };
}
finally
{
	held.Release();
	bootstrap.Enabled = false;
	if ( Game.IsEditor )
	{
		if ( originalOwners is null ) FileSystem.Data.DeleteFile( ownersPath );
		else FileSystem.Data.WriteAllText( ownersPath, originalOwners );
	}
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = true;
}
