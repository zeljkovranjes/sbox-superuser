if ( !Networking.IsHost || !Application.IsDedicatedServer && !Game.IsEditor ) throw new InvalidOperationException( "Isolated host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() ).OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalEnabled = bootstrap.Enabled;
var originalSwitch = bootstrap.AllowEditorTesting;
var originalCapacity = ConsoleSystem.GetValue( "superuser_example_capacity" );
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
object files = null;
System.Reflection.PropertyInfo checkpoint = null;
try
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = Game.IsEditor;
	bootstrap.Enabled = true;
	var started = await SboxSuperuser.Superuser.RegisterModulesAsync( new[] { SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.CreateModule() }, cancellationToken );
	Require( started.Status == SboxSuperuser.CommandStatus.Success, "Settings startup failed." );
	using var service = SboxSuperuser.Superuser.CreateCommandService( "t15.failure", new[] { "example.settings", "admin.help" } );
	using var denied = SboxSuperuser.Superuser.CreateCommandService( "t15.denied", new[] { "admin.help" } );
	var key = new SboxSuperuser.StoreKey( "superuser.settings", "example.capacity" );
	var store = SboxSuperuser.Superuser.GetStore();
	var first = await service.ExecuteAsync( new SboxSuperuser.CommandRequest( "settings.set.example.capacity", new[] { "12" }, Guid.NewGuid() ), cancellationToken );
	Require( first.Status == SboxSuperuser.CommandStatus.Success, "Seed write failed." );
	var before = await store.ReadAsync( key, 1, cancellationToken );
	var rejected = await denied.ExecuteAsync( new SboxSuperuser.CommandRequest( "settings.set.example.capacity", new[] { "13" }, Guid.NewGuid() ), cancellationToken );
	Require( rejected.Status == SboxSuperuser.CommandStatus.Denied, "Missing capability changed a setting." );
	var listing = await denied.ExecuteAsync( new SboxSuperuser.CommandRequest( "settings.list", Array.Empty<string>(), Guid.NewGuid() ), cancellationToken );
	Require( !listing.Data.Keys.Any( name => name.StartsWith( "row" ) ), "List revealed unauthorized bindings." );
	var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
	var coordinator = store.GetType().GetField( "_coordinator", flags ).GetValue( store );
	files = coordinator.GetType().GetField( "_files", flags ).GetValue( coordinator );
	checkpoint = files.GetType().GetProperty( "WriteCheckpoint", flags );
	checkpoint.SetValue( files, (Func<string, CancellationToken, Task>)((path, _) => path.Contains( "/superuser.settings/example.capacity/g-" )
		? Task.FromException( new System.IO.IOException( "Injected setting persistence failure" ) ) : Task.CompletedTask) );
	var failed = await service.ExecuteAsync( new SboxSuperuser.CommandRequest( "settings.set.example.capacity", new[] { "13" }, Guid.NewGuid() ), cancellationToken );
	checkpoint.SetValue( files, null );
	Require( failed.Status == SboxSuperuser.CommandStatus.StorageUnavailable, "Failed persistent setting falsely succeeded: " + failed.Status );
	Require( ConsoleSystem.GetValue( "superuser_example_capacity" ) == "12", "Storage failure did not restore active native value." );
	Require( (await store.ReadAsync( key, 1, cancellationToken )).Document.Revision == before.Document.Revision, "Storage failure changed committed desired state." );
	var consoleVerified = false;
	if ( Application.IsDedicatedServer )
	{
		ConsoleSystem.Run( "admin", new object[] { "settings", "set", "example.capacity", "14" } );
		for ( var attempt = 0; attempt < 100 && (await store.ReadAsync( key, 1, cancellationToken )).Document.Revision == before.Document.Revision; attempt++ )
			await Task.Delay( 25, cancellationToken );
		Require( ConsoleSystem.GetValue( "superuser_example_capacity" ) == "14", "Actual dedicated console settings route failed." );
		var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
		SboxSuperuser.ActorIdentity Authenticate()
		{
			var parameters = new object[] { null };
			return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, parameters ) ? (SboxSuperuser.ActorIdentity)parameters[ 0 ] : null;
		}
		var page = await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
		{
			(Func<SboxSuperuser.ActorIdentity>)Authenticate, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
			(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( command: "settings.set.example.capacity" ), CancellationToken.None
		} );
		Require( page.Entries.Any( entry => entry.ActorKind == SboxSuperuser.ActorKind.DedicatedConsole && entry.Outcome == SboxSuperuser.CommandStatus.Success ), "Console setting lacked attributed audit evidence." );
		consoleVerified = true;
	}
	return new { Pass = true, Dedicated = Application.IsDedicatedServer, Failure = failed.Status, NativeConsole = consoleVerified, Denied = rejected.Status };
}
finally
{
	checkpoint?.SetValue( files, null );
	bootstrap.Enabled = false;
	ConsoleSystem.SetValue( "superuser_example_capacity", originalCapacity );
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = originalEnabled;
}
