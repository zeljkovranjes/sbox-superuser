// Actual dedicated scene transitions with live modules, pending commands and committed state.
if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Isolated actual dedicated host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
object Runtime() => typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
async Task<SboxSuperuser.CommandResult> Local( string command, params string[] args )
{
	var runtime = Runtime();
	var dispatcher = runtime.GetType().GetProperty( "Dispatcher", flags ).GetValue( runtime );
	SboxSuperuser.ActorIdentity Authenticate()
	{
		var values = new object[] { null };
		return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, values ) ? (SboxSuperuser.ActorIdentity)values[ 0 ] : null;
	}
	return await (Task<SboxSuperuser.CommandResult>)dispatcher.GetType().GetMethod( "SubmitAsync", flags ).Invoke( dispatcher, new object[]
	{
		(Func<SboxSuperuser.ActorIdentity>)Authenticate, SboxSuperuser.CommandOrigin.Console,
		new SboxSuperuser.CommandRequest( command, args, Guid.NewGuid() ), cancellationToken, null
	} );
}
Require( (await Local( "group.create", "lifecycle" )).Status == SboxSuperuser.CommandStatus.Success, "Policy seed failed." );
Require( (await Local( "group.permission.allow", "lifecycle", "admin.help" )).Status == SboxSuperuser.CommandStatus.Success, "Permission seed failed." );
Require( (await Local( "ban", "76561198000000061", "permanent", "T17 scene retention" )).Status == SboxSuperuser.CommandStatus.Success, "Ban seed failed." );
Require( (await Local( "mute", "76561198000000061", "1h", "T17 original UTC expiry" )).Status == SboxSuperuser.CommandStatus.Success, "Mute seed failed." );
var keys = new[] { new SboxSuperuser.StoreKey( "security", "policy" ), new SboxSuperuser.StoreKey( "moderation", "history" ) };
var proof = new System.Collections.Generic.Dictionary<string, string>();
foreach ( var key in keys ) proof.Add( key.ModuleId, (await SboxSuperuser.Superuser.GetStore().ReadAsync( key, 1, cancellationToken )).Document.PayloadJson );
FileSystem.Data.CreateDirectory( "superuser-t17" );
FileSystem.Data.WriteAllText( "superuser-t17/scene-proof.json", Json.Serialize( proof ) );
var ids = new System.Collections.Generic.HashSet<Guid>();
for ( var iteration = 0; iteration < 3; iteration++ )
{
	var runtime = Runtime();
	var id = SboxSuperuser.Superuser.GetDiagnostics().RuntimeId.Value;
	Require( ids.Add( id ), "Runtime ID reused." );
	var pulses = new SuperuserCompatibilityLab.Engine.Modules.SamplePulseSource();
	var sample = new SuperuserCompatibilityLab.Engine.Modules.SampleCounterModule( pulses );
	var held = new SuperuserCompatibilityLab.Engine.Modules.HeldCommandModule();
	Require( (await SboxSuperuser.Superuser.RegisterModulesAsync( new SboxSuperuser.IAdminModule[] { sample, held, SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.CreateModule() }, cancellationToken )).Status == SboxSuperuser.CommandStatus.Success, "Module startup failed." );
	using var service = SboxSuperuser.Superuser.CreateCommandService( "t17.lifecycle", new[] { "sample.counter.use", "sample.held.use" } );
	Require( (await service.ExecuteAsync( new SboxSuperuser.CommandRequest( "sample.count", Array.Empty<string>(), Guid.NewGuid() ) )).Status == SboxSuperuser.CommandStatus.Success, "Sample command failed." );
	pulses.Raise();
	Require( sample.Executions == 1 && sample.Pulses == 1 && pulses.Listeners == 1, "Duplicate handler before scene change." );
	var pending = service.ExecuteAsync( new SboxSuperuser.CommandRequest( "sample.hold", Array.Empty<string>(), Guid.NewGuid() ) );
	for ( var i = 0; i < 100 && !held.Entered; i++ ) await Task.Delay( 25, cancellationToken );
	Require( held.Entered, "Held command did not start." );
	var oldStore = SboxSuperuser.Superuser.GetStore();
	var options = new SceneLoadOptions();
	Require( options.SetScene( "scenes/bootstrap.scene" ), "Scene unavailable." );
	Game.ChangeScene( options );
	for ( var i = 0; i < 100 && (SboxSuperuser.Superuser.GetDiagnostics().RuntimeId is null || SboxSuperuser.Superuser.GetDiagnostics().RuntimeId == id); i++ ) await Task.Delay( 50, cancellationToken );
	Require( SboxSuperuser.Superuser.GetDiagnostics().RuntimeId is Guid next && next != id && SboxSuperuser.Superuser.GetDiagnostics().BootstrapCount == 1, "Fresh runtime missing." );
	Require( (bool)runtime.GetType().GetField( "_disposed", flags ).GetValue( runtime ), "Old runtime retained authority." );
	Require( pulses.Listeners == 0 && sample.Stops == 1 && held.CancellationSeen, "Scene change leaked listeners or failed cancellation." );
	Require( (await oldStore.ReadAsync( keys[ 0 ], 1, cancellationToken )).Status == SboxSuperuser.StoreReadStatus.Unavailable, "Old store remained available." );
	Require( (await service.ExecuteAsync( new SboxSuperuser.CommandRequest( "sample.count", Array.Empty<string>(), Guid.NewGuid() ) )).Status == SboxSuperuser.CommandStatus.Unsupported, "Old caller remained available." );
	held.Release();
	Require( (await pending).Status == SboxSuperuser.CommandStatus.Unknown && held.Stops == 1, "Interrupted operation did not drain exactly once." );
	Require( !SboxSuperuser.Superuser.GetCommandDescriptors().Any( command => command.ModuleId is "sample.counter" or "sample.held" or "superuser.settings" ), "Stale commands survived scene replacement." );
	foreach ( var key in keys ) Require( (await SboxSuperuser.Superuser.GetStore().ReadAsync( key, 1, cancellationToken )).Document.PayloadJson == proof[ key.ModuleId ], "Committed state changed across scene replacement." );
	Log.Info( $"[T17] Scene replacement {iteration + 1} passed; old={id}; new={SboxSuperuser.Superuser.GetDiagnostics().RuntimeId}" );
}
return new { Pass = true, Scenes = 3, OldHandlesRevoked = true, PendingOutcomes = "Unknown", PolicyAndModerationPreserved = true };
