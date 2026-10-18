if ( !Game.IsEditor || !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Isolated editor host required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalSwitch = bootstrap.AllowEditorTesting;
var probe = TypeLibrary.GetType( "SuperuserCompatibilityLab.Engine.Commands.DispatchProbe" ).Create<SuperuserCompatibilityLab.IDispatchProbe>();
SboxSuperuser.CommandRegistrationScope registration = null;
SboxSuperuser.CommandService service = null;
void Require( bool value, string message )
{
	if ( !value ) throw new InvalidOperationException( message );
}
SboxSuperuser.CommandRequest Request( string command, Guid? id = null, string[] arguments = null ) => new( command, arguments ?? Array.Empty<string>(), id ?? Guid.NewGuid() );
Task<SboxSuperuser.CommandResult> Execute( string command, Guid? id = null, CancellationToken cancellation = default )
	=> SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local, Request( command, id ), SboxSuperuser.CommandOrigin.Chat, cancellation );
try
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = true;
	bootstrap.Enabled = true;
	registration = SboxSuperuser.Superuser.BeginCommandRegistration( "test.dispatch" );
	registration.RegisterService<SuperuserCompatibilityLab.IDispatchProbe>( probe );
	registration.DiscoverCommands();
	registration.Commit();
	var id = Guid.NewGuid();
	Require( (await Execute( "probe.increment", id )).Status == SboxSuperuser.CommandStatus.Success, "Authenticated player did not execute." );
	Require( (await Execute( "probe.increment", id )).Status == SboxSuperuser.CommandStatus.Success && probe.Count == 1, "Retry repeated the mutation." );
	Require( (await SboxSuperuser.Superuser.ExecuteCommandAsync( null, Request( "probe.increment" ), SboxSuperuser.CommandOrigin.Chat )).Status == SboxSuperuser.CommandStatus.Denied, "Null caller became console." );
	Require( (await Execute( "probe.fail" )).Status == SboxSuperuser.CommandStatus.Unknown, "Mutation exception became success or definite rollback." );
	var failure = await Execute( "probe.fail" );
	Require( !failure.Message.Contains( "test-secret" ), "Exception text leaked." );
	Require( (await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
		Request( "probe.punish", arguments: new[] { Connection.Local.SteamId.ToString() } ), SboxSuperuser.CommandOrigin.Chat )).Status == SboxSuperuser.CommandStatus.Denied,
		"Punitive self-targeting was allowed." );

	probe.Hold();
	var held = Execute( "probe.hold" );
	var queued = Execute( "probe.increment" );
	Require( !queued.IsCompleted && probe.Count == 1, "Mutation did not wait behind active work." );
	probe.Release();
	Require( (await held).Status == SboxSuperuser.CommandStatus.Success && (await queued).Status == SboxSuperuser.CommandStatus.Success && probe.Count == 2,
		"Queue did not resume the compiled capability on the main thread." );

	service = SboxSuperuser.Superuser.CreateCommandService( "test.service", new[] { "admin.help" } );
	probe.Hold();
	held = Execute( "probe.hold" );
	var revoked = service.ExecuteAsync( Request( "probe.increment" ) );
	service.Dispose();
	probe.Release();
	await held;
	Require( (await revoked).Status == SboxSuperuser.CommandStatus.Denied && probe.Count == 2, "Revoked service executed queued work." );

	probe.Hold();
	using var cancelActive = new CancellationTokenSource();
	using var cancelQueued = new CancellationTokenSource();
	held = Execute( "probe.hold", cancellation: cancelActive.Token );
	var cancelled = Execute( "probe.increment", cancellation: cancelQueued.Token );
	cancelQueued.Cancel();
	Require( (await cancelled).Status == SboxSuperuser.CommandStatus.Cancelled, "Queued cancellation failed." );
	cancelActive.Cancel();
	probe.Release();
	Require( (await held).Status == SboxSuperuser.CommandStatus.Unknown && probe.Count == 2, "Active cancellation promised rollback or changed queued state." );
	registration.Dispose();
	Require( (await Execute( "probe.increment" )).Status == SboxSuperuser.CommandStatus.NotFound, "Disposed module remained executable." );
	Log.Info( "[T09] editor dispatcher PASS: authenticated player, compiled capability, retry, queue, cancellation, revocation and safe failures" );
	return new { Pass = true, Mutations = probe.Count };
}
finally
{
	probe.Release();
	service?.Dispose();
	registration?.Dispose();
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = true;
}
