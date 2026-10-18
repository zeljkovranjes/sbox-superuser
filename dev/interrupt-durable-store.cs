// Trusted development script. Pauses the actual adapter with a partial stream open; kill ONLY the isolated server PID.
// Select the checkpoint by creating superuser/t04-interrupt-phase.txt containing exactly candidate or commit.
if ( !Application.IsDedicatedServer ) throw new InvalidOperationException( "Isolated dedicated server required." );
var phase = FileSystem.Data.ReadAllText( "superuser/t04-interrupt-phase.txt" ).Trim();
if ( phase != "candidate" && phase != "commit" ) throw new InvalidOperationException( "Invalid test phase." );
var store = SboxSuperuser.Superuser.GetStore() ?? throw new InvalidOperationException( "Bootstrap unavailable." );
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var coordinator = store.GetType().GetField( "_coordinator", flags ).GetValue( store );
var files = coordinator.GetType().GetField( "_files", flags ).GetValue( coordinator );
var checkpoint = files.GetType().GetProperty( "WriteCheckpoint", flags );
checkpoint.SetValue( files, new Func<string, System.Threading.CancellationToken, System.Threading.Tasks.Task>( async (path, cancellation) =>
{
	if ( !path.Contains( phase == "candidate" ? "/g-" : "/c-" ) ) return;
	Log.Warning( $"[T04] INTERRUPTION CHECKPOINT phase={phase}; partial stream flushed and open; path={path}" );
	await System.Threading.Tasks.Task.Delay( 300000, cancellation );
} ) );
try
{
	var key = new SboxSuperuser.StoreKey( "t04.restart", "records" );
	var token = System.Threading.CancellationToken.None;
	var current = await store.ReadAsync( key, 1, token );
	if ( current.Status != SboxSuperuser.StoreReadStatus.Found ) throw new InvalidOperationException( current.Status.ToString() );
	var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.DedicatedConsole,
		"dedicated-console", SboxSuperuser.CommandOrigin.Console, "test.interrupt", DateTimeOffset.UtcNow );
	var candidate = new SboxSuperuser.PolicyTransaction( "{\"token\":\"unacknowledged-candidate\",\"owners\":[],\"bans\":[]}", new[] { intent } );
	return await store.WriteAsync( candidate.CreateWriteRequest( key, current.Document.Revision ), token );
}
finally
{
	checkpoint.SetValue( files, null );
}
