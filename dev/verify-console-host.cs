// Run on the editor host after verify-console-client.cs; set submissionsPath to its saved JSON output.
if ( !Game.IsEditor || !Networking.IsHost ) throw new InvalidOperationException( "Isolated editor host required." );
const string submissionsPath = "E:/.sbox/02-plugins/sbox-superuser/dev/out/t11-client-submissions.json";
using var submissions = System.Text.Json.JsonDocument.Parse( System.IO.File.ReadAllText( submissionsPath ) );
var help = submissions.RootElement.GetProperty( "HelpRequest" ).GetGuid();
var denied = submissions.RootElement.GetProperty( "DeniedRequest" ).GetGuid();
var caller = submissions.RootElement.GetProperty( "Caller" ).GetString();
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
async Task<SboxSuperuser.AuditPage> Query( Guid request ) => await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
{
	(Func<SboxSuperuser.ActorIdentity>)(() => SboxSuperuser.Superuser.TryGetPlayerIdentity( Connection.Local, out var actor ) ? actor : null),
	(Func<SboxSuperuser.ActorIdentity, bool>)(_ => true), (Func<SboxSuperuser.ActorIdentity, bool>)(_ => false),
	new SboxSuperuser.AuditQuery( requestId: request ), CancellationToken.None
} );
var helpRows = (await Query( help )).Entries;
var deniedRows = (await Query( denied )).Entries;
if ( helpRows.Count != 1 || helpRows.Single().Outcome != SboxSuperuser.CommandStatus.Success || helpRows.Single().ActorSubject != caller
	|| helpRows.Single().ActorKind != SboxSuperuser.ActorKind.Player || deniedRows.Count != 1 || deniedRows.Single().Outcome != SboxSuperuser.CommandStatus.Denied
	|| deniedRows.Single().ActorSubject != caller ) throw new InvalidOperationException( "Native retry or authenticated attribution failed." );
return new { Pass = true, HelpRows = helpRows.Count, DeniedRows = deniedRows.Count, Caller = caller };
