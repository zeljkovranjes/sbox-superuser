// Trusted lab script: kill only the owned dedicated PID after t10-crash-checkpoint.json appears.
if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Actual isolated dedicated server required." );
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
if ( runtime is null ) throw new InvalidOperationException( "Dedicated bootstrap unavailable." );
var store = runtime.GetType().GetProperty( "Store", flags ).GetValue( runtime );
var coordinator = store.GetType().GetField( "_coordinator", flags ).GetValue( store );
var files = coordinator.GetType().GetField( "_files", flags ).GetValue( coordinator );
var checkpoint = files.GetType().GetProperty( "WriteCheckpoint", flags );
var dispatcher = runtime.GetType().GetProperty( "Dispatcher", flags ).GetValue( runtime );
var probe = TypeLibrary.GetType( "SuperuserCompatibilityLab.Engine.Commands.DispatchProbe" ).Create<SuperuserCompatibilityLab.IDispatchProbe>();
using var registration = SboxSuperuser.Superuser.BeginCommandRegistration( "test.dispatch" );
registration.RegisterService<SuperuserCompatibilityLab.IDispatchProbe>( probe );
registration.DiscoverCommands();
registration.Commit();
var requestId = Guid.NewGuid();
checkpoint.SetValue( files, (Func<string, CancellationToken, Task>)(async ( path, token ) =>
{
	if ( !path.Contains( "/audit/journal/g-" ) || probe.Count != 1 ) return;
	FileSystem.Data.WriteAllText( "superuser/t10-crash-checkpoint.json", System.Text.Json.JsonSerializer.Serialize( new { requestId, mutations = probe.Count, path } ) );
	Log.Warning( "[T10] OUTCOME INTERRUPTION CHECKPOINT: intent committed, compiled handler mutated once, partial outcome stream flushed and open." );
	await Task.Delay( 60000, cancellationToken );
} ) );
SboxSuperuser.ActorIdentity AuthenticateLocal()
{
	var arguments = new object[] { null };
	return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, arguments )
		? (SboxSuperuser.ActorIdentity)arguments[ 0 ] : null;
}
try
{
	return await (Task<SboxSuperuser.CommandResult>)dispatcher.GetType().GetMethod( "SubmitAsync", flags ).Invoke( dispatcher, new object[]
	{
		(Func<SboxSuperuser.ActorIdentity>)AuthenticateLocal, SboxSuperuser.CommandOrigin.Console,
		new SboxSuperuser.CommandRequest( "probe.increment", Array.Empty<string>(), requestId ), CancellationToken.None, null
	} );
}
finally
{
	checkpoint.SetValue( files, null );
}
