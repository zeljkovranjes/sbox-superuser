// Trusted local measurement. No network/identity claim is inferred from policy microbenchmarks.
if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Isolated actual dedicated host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
object Summary( double[] values )
{
	Array.Sort( values );
	return new { Count = values.Length, Unit = "microseconds", Mean = values.Average(), P50 = values[ values.Length / 2 ], P95 = values[ (int)Math.Ceiling( values.Length * 0.95 ) - 1 ], Maximum = values[ ^1 ] };
}
double Microseconds( long start ) => (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000000.0 / System.Diagnostics.Stopwatch.Frequency;
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
Require( runtime is not null, "Dedicated runtime unavailable." );
var policy = runtime.GetType().GetProperty( "Policy", flags ).GetValue( runtime );
var explain = policy.GetType().GetMethod( "Explain", flags ).CreateDelegate<Func<SboxSuperuser.ActorIdentity, string, SboxSuperuser.PermissionExplanation>>( policy );
var actorConstructor = typeof( SboxSuperuser.ActorIdentity ).GetConstructor( flags, null, new[] { typeof( SboxSuperuser.ActorKind ), typeof( string ) }, null );
var actors = Enumerable.Range( 0, 8 ).Select( index => (SboxSuperuser.ActorIdentity)actorConstructor.Invoke( new object[] { SboxSuperuser.ActorKind.Player, (76561198000000100UL + (ulong)index).ToString() } ) ).ToArray();
var inputs = new[] { "help", "ban 76561198000000100 1h \"Reason with spaces\"", "settings set example.capacity 16", "example \"" + new string( 'x', 512 ) + "\"" };
for ( var i = 0; i < 1000; i++ )
{
	Require( SboxSuperuser.Superuser.TokenizeCommandText( inputs[ i % inputs.Length ] ).IsSuccess, "Warmup grammar failed." );
	explain( actors[ i % actors.Length ], i % 2 == 0 ? "admin.help" : "admin.players.ban" );
}
const int samples = 20000;
var parseTimes = new double[ samples ];
var permissionTimes = new double[ samples ];
var allowed = 0;
for ( var i = 0; i < samples; i++ )
{
	var start = System.Diagnostics.Stopwatch.GetTimestamp();
	var parsed = SboxSuperuser.Superuser.TokenizeCommandText( inputs[ i % inputs.Length ] );
	parseTimes[ i ] = Microseconds( start );
	Require( parsed.IsSuccess, "Measured tokenization failed." );
	start = System.Diagnostics.Stopwatch.GetTimestamp();
	var decision = explain( actors[ i % actors.Length ], i % 2 == 0 ? "admin.help" : "admin.players.ban" );
	permissionTimes[ i ] = Microseconds( start );
	if ( decision.IsAllowed ) allowed++;
	if ( i % 500 == 499 ) await Task.Yield();
}
Require( allowed == samples / 2, "Cached authorization answers changed." );
var malformed = new[] { "\"unterminated", "help\nban", new string( 'x', 4097 ), string.Join( " ", Enumerable.Repeat( "x", 65 ) ), "test " + new string( 'x', 513 ) };
foreach ( var text in malformed )
{
	var result = SboxSuperuser.Superuser.TokenizeCommandText( text );
	Require( !result.IsSuccess && result.Tokens.Count == 0, "Malformed input exposed partial tokens." );
}
var random = new Random( 19 );
const string alphabet = " abcXYZ019\\\"\t\n/!";
var fuzzAccepted = 0;
for ( var i = 0; i < 2000; i++ )
{
	var chars = new char[ random.Next( 0, 600 ) ];
	for ( var j = 0; j < chars.Length; j++ ) chars[ j ] = alphabet[ random.Next( alphabet.Length ) ];
	var parsed = SboxSuperuser.Superuser.TokenizeCommandText( new string( chars ) );
	Require( parsed.Tokens.Count <= 64 && parsed.Tokens.All( token => token.Length <= 512 ), "Fuzz input escaped token limits." );
	if ( parsed.IsSuccess ) fuzzAccepted++;
	else Require( parsed.Tokens.Count == 0, "Rejected fuzz input exposed tokens." );
	if ( i % 100 == 99 ) await Task.Yield();
}
var store = SboxSuperuser.Superuser.GetStore();
var key = new SboxSuperuser.StoreKey( "t19.measure", "persistence" );
Require( (await store.ReadAsync( key, 1, cancellationToken )).Status == SboxSuperuser.StoreReadStatus.Missing, "Use a fresh measurement key." );
var writeTimes = new double[ 100 ];
var payloadBytes = 0;
for ( var i = 0; i < 105; i++ )
{
	var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.InternalService,
		"t19.measure", SboxSuperuser.CommandOrigin.InternalService, "measure.write", DateTimeOffset.UtcNow );
	var transaction = new SboxSuperuser.PolicyTransaction( "{\"padding\":\"" + new string( 'x', 1024 ) + "\",\"sequence\":" + i + "}", new[] { intent } );
	var request = transaction.CreateWriteRequest( key, i );
	payloadBytes = System.Text.Encoding.UTF8.GetByteCount( request.Document.PayloadJson );
	var start = System.Diagnostics.Stopwatch.GetTimestamp();
	var written = await store.WriteAsync( request, cancellationToken );
	var elapsed = Microseconds( start );
	Require( written.Status == SboxSuperuser.StoreWriteStatus.Committed && written.CommittedRevision == i + 1, "Measured write was not committed." );
	if ( i >= 5 ) writeTimes[ i - 5 ] = elapsed;
	await Task.Yield();
}
Require( (await store.ReadAsync( key, 1, cancellationToken )).Document.Revision == 105, "Final measured revision differs." );
var report = new { Pass = true, Machine = "DESKTOP-7LI35HI", Engine = "26.10.13a", Parsing = Summary( parseTimes ), CachedHostPolicy = Summary( permissionTimes ), Persistence = Summary( writeTimes ), payloadBytes, WarmupCalls = 1000, WarmupWrites = 5, MalformedCases = malformed.Length, FuzzCases = 2000, fuzzAccepted, Note = "Individual operation timing; scheduler yields between batches are excluded. Persistence includes generation/marker close and readback; no fsync claim." };
FileSystem.Data.CreateDirectory( "superuser-t19" );
FileSystem.Data.WriteAllText( "superuser-t19/measurements.json", Json.Serialize( report ) );
return report;
