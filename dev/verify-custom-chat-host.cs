if ( !Networking.IsHost ) throw new InvalidOperationException( "Isolated host required." );
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
var sender = Connection.All.Single( connection => connection.SteamId == 90071996842377217 );
SboxSuperuser.ActorIdentity Authenticate() => SboxSuperuser.Superuser.TryGetPlayerIdentity( sender, out var actor ) ? actor : null;
var page = await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
{
	(Func<SboxSuperuser.ActorIdentity>)Authenticate, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
	(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( command: "example.increment" ), CancellationToken.None
} );
if ( page.Entries.Count != 2 || page.Entries.Any( row => row.ActorKind != SboxSuperuser.ActorKind.Player
	|| row.ActorSubject != sender.SteamId.ToString() || row.Origin != SboxSuperuser.CommandOrigin.Chat || row.Outcome != SboxSuperuser.CommandStatus.Success ) )
	throw new InvalidOperationException( "Custom backends did not share two correctly attributed executions." );
var decision = SboxSuperuser.Superuser.CheckOrdinaryChat( sender );
if ( decision.Status != SboxSuperuser.CommandStatus.Success || SboxSuperuser.Superuser.CheckOrdinaryChat( null ).Status != SboxSuperuser.CommandStatus.Denied )
	throw new InvalidOperationException( "Ordinary chat authentication failed." );
return new { Pass = true, OrdinaryDecision = decision.Status, Requests = page.Entries.Select( row => row.RequestId ).ToArray() };
