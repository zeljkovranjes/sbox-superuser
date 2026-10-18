if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Actual isolated dedicated server required." );
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
using var checkpoint = System.Text.Json.JsonDocument.Parse( FileSystem.Data.ReadAllText( "superuser/t10-crash-checkpoint.json" ) );
var id = checkpoint.RootElement.GetProperty( "requestId" ).GetGuid();
if ( checkpoint.RootElement.GetProperty( "mutations" ).GetInt32() != 1 ) throw new InvalidOperationException( "No observed mutation checkpoint." );
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
SboxSuperuser.ActorIdentity AuthenticateLocal()
{
	var arguments = new object[] { null };
	return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, arguments )
		? (SboxSuperuser.ActorIdentity)arguments[ 0 ] : null;
}
var page = await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
{
	(Func<SboxSuperuser.ActorIdentity>)AuthenticateLocal, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
	(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( requestId: id ), CancellationToken.None
} );
var entry = page.Entries.Single();
if ( page.Status != SboxSuperuser.CommandStatus.Success || entry.Outcome != SboxSuperuser.CommandStatus.Unknown
	|| entry.CompletedUtc is not null || entry.ActorKind != SboxSuperuser.ActorKind.DedicatedConsole || entry.Command != "probe.increment" )
	throw new InvalidOperationException( "Interrupted action lost attribution or falsely acquired success." );
Log.Info( "[T10] dedicated restart PASS: attributable intent remains Unknown after process termination during the outcome write." );
return new { Pass = true, entry.RequestId, entry.Id, entry.Outcome, entry.CompletedUtc, entry.ActorSubject, entry.Command };
