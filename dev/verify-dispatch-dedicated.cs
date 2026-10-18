if ( !Application.IsDedicatedServer || !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Actual dedicated host required." );
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var assembly = typeof( SboxSuperuser.Superuser ).Assembly;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
if ( runtime is null ) throw new InvalidOperationException( "Dedicated runtime not initialized." );
var dispatcher = runtime.GetType().GetProperty( "Dispatcher", flags ).GetValue( runtime );
var submit = dispatcher.GetType().GetMethod( "SubmitAsync", flags );
SboxSuperuser.ActorIdentity AuthenticateLocal()
{
	var parameters = new object[] { null };
	return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, parameters )
		? (SboxSuperuser.ActorIdentity)parameters[ 0 ] : null;
}
Task<SboxSuperuser.CommandResult> Local( string command, Guid? id = null, string[] arguments = null )
	=> (Task<SboxSuperuser.CommandResult>)submit.Invoke( dispatcher, new object[]
	{
		(Func<SboxSuperuser.ActorIdentity>)AuthenticateLocal, SboxSuperuser.CommandOrigin.Console,
		new SboxSuperuser.CommandRequest( command, arguments ?? Array.Empty<string>(), id ?? Guid.NewGuid() ), CancellationToken.None, null
	} );
void Require( bool value, string message )
{
	if ( !value ) throw new InvalidOperationException( message );
}
var probe = TypeLibrary.GetType( "SuperuserCompatibilityLab.Engine.Commands.DispatchProbe" ).Create<SuperuserCompatibilityLab.IDispatchProbe>();
using var registration = SboxSuperuser.Superuser.BeginCommandRegistration( "test.dispatch" );
registration.RegisterService<SuperuserCompatibilityLab.IDispatchProbe>( probe );
registration.DiscoverCommands();
registration.Commit();
var id = Guid.NewGuid();
Require( (await Local( "probe.increment", id )).Status == SboxSuperuser.CommandStatus.Success, "Dedicated local identity failed." );
Require( (await Local( "probe.increment", id )).Status == SboxSuperuser.CommandStatus.Success && probe.Count == 1, "Dedicated retry repeated mutation." );
Require( (await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
	new SboxSuperuser.CommandRequest( "probe.increment", Array.Empty<string>(), Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console )).Status == SboxSuperuser.CommandStatus.Denied,
	"Dedicated host connection became a player credential." );
using var service = SboxSuperuser.Superuser.CreateCommandService( "test.service", new[] { "admin.help" } );
probe.Hold();
var held = Local( "probe.hold" );
var queued = service.ExecuteAsync( new SboxSuperuser.CommandRequest( "probe.increment", Array.Empty<string>(), Guid.NewGuid() ) );
Require( !queued.IsCompleted, "Dedicated mutation bypassed queue." );
probe.Release();
Require( (await held).Status == SboxSuperuser.CommandStatus.Success && (await queued).Status == SboxSuperuser.CommandStatus.Success && probe.Count == 2,
	"Dedicated queue failed to resume compiled game capability." );
Require( (await Local( "probe.punish", arguments: new[] { "76561198000000001" } )).Status == SboxSuperuser.CommandStatus.Success && probe.Count == 3,
	"Dedicated local authority could not pass offline target policy." );
var failure = await Local( "probe.fail" );
Require( failure.Status == SboxSuperuser.CommandStatus.Unknown && !failure.Message.Contains( "test-secret" ), "Dedicated handler failure leaked or reported success." );
Log.Info( "[T09] dedicated dispatcher PASS: separate local identity, player-route refusal, retry, service queue, offline target and safe failure" );
return new { Pass = true, Mutations = probe.Count, Dedicated = Application.IsDedicatedServer };
