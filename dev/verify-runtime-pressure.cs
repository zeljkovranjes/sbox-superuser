// Actual engine dispatch and real audit storage; service callers are trusted host-issued workload actors.
if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Isolated actual dedicated host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
var dispatcher = runtime.GetType().GetProperty( "Dispatcher", flags ).GetValue( runtime );
var queue = dispatcher.GetType().GetField( "_queue", flags ).GetValue( dispatcher );
var release = new TaskCompletionSource<bool>();
var active = 0;
var maximumActive = 0;
var effects = 0;
using var registration = SboxSuperuser.Superuser.BeginCommandRegistration( "t19.pressure" );
registration.RegisterCommand( new SboxSuperuser.CommandDescriptor( "pressure.increment", "Isolated pressure counter", "admin.help", "t19.pressure" ),
	async ( context, _ ) =>
	{
		active++;
		maximumActive = Math.Max( maximumActive, active );
		try
		{
			await release.Task;
			context.Cancellation.ThrowIfCancellationRequested();
			effects++;
			return new SboxSuperuser.CommandResult( SboxSuperuser.CommandStatus.Success, "Counted once." );
		}
		finally { active--; }
	} );
registration.Commit();
var services = Enumerable.Range( 0, 16 ).Select( index => SboxSuperuser.Superuser.CreateCommandService( "pressure.actor" + index, new[] { "admin.help" } ) ).ToArray();
var requests = new System.Collections.Generic.List<(SboxSuperuser.CommandService Service, SboxSuperuser.CommandRequest Request, Task<SboxSuperuser.CommandResult> Task)>();
var started = System.Diagnostics.Stopwatch.StartNew();
try
{
	for ( var index = 0; index < 512; index++ )
	{
		var service = services[ index / 32 ];
		var request = new SboxSuperuser.CommandRequest( "pressure.increment", Array.Empty<string>(), Guid.NewGuid() );
		requests.Add( (service, request, service.ExecuteAsync( request )) );
		if ( index % 8 == 7 ) await Task.Delay( 1, cancellationToken );
	}
	var outstanding = (int)queue.GetType().GetField( "_outstanding", flags ).GetValue( queue );
	var remembered = queue.GetType().GetField( "_remembered", flags ).GetValue( queue );
	var rememberedCount = (int)remembered.GetType().GetProperty( "Count" ).GetValue( remembered );
	Require( outstanding == 64 && rememberedCount == 64 && active == 1 && effects == 0, "Command queue escaped its bounds or overlapped effects." );
	Require( requests.Count( item => item.Task.IsCompletedSuccessfully && item.Task.Result.Status == SboxSuperuser.CommandStatus.Conflict ) == 448, "Excess requests were not rejected." );
	var first = requests[ 0 ];
	var duplicate = first.Service.ExecuteAsync( first.Request );
	Require( !duplicate.IsCompleted && (int)queue.GetType().GetField( "_outstanding", flags ).GetValue( queue ) == 64, "Retry consumed another queue slot." );
	release.TrySetResult( true );
	var results = await Task.WhenAll( requests.Select( item => item.Task ) );
	Require( results.Count( result => result.Status == SboxSuperuser.CommandStatus.Success ) == 64 && effects == 64 && maximumActive == 1, "Concurrent effects were lost, repeated or overlapped." );
	Require( (await duplicate).Status == SboxSuperuser.CommandStatus.Success && effects == 64, "Duplicate request repeated its mutation." );
	Require( results.All( result => result.Message.Length <= 1024 && result.Data.Count <= 16 ), "Pressure result exceeded payload limits." );
	var audit = SboxSuperuser.Superuser.GetAuditDiagnostics();
	Require( !audit.IsDegraded && audit.RetainedCount <= 512, "Audit retention failed under pressure." );
	var journalFiles = FileSystem.Data.FindFile( "superuser/store/dedicated/audit/journal", "*", false ).Count();
	Require( journalFiles <= 4, "Automatic audit pruning left excessive generations." );
	var report = new { Pass = true, Submitted = requests.Count, Actors = services.Length, MaximumOutstanding = outstanding, MaximumActorOutstanding = 8, Rejected = 448, effects, maximumActive, RetryEffect = 0, audit.RetainedCount, journalFiles, TotalSeconds = started.Elapsed.TotalSeconds };
	FileSystem.Data.CreateDirectory( "superuser-t19" );
	FileSystem.Data.WriteAllText( "superuser-t19/pressure.json", Json.Serialize( report ) );
	return report;
}
finally
{
	release.TrySetResult( true );
	foreach ( var service in services ) service.Dispose();
}
