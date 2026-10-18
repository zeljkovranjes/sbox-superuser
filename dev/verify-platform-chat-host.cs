// Read actual host audit rows after verify-platform-chat-client.cs runs once with an eligible bootstrap.
if ( !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Run on the isolated host." );
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var sender = Connection.All.Single( connection => connection.SteamId == 90071996842377217 );
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
SboxSuperuser.ActorIdentity Authenticate() => SboxSuperuser.Superuser.TryGetPlayerIdentity( sender, out var actor ) ? actor : null;
var page = await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
{
	(Func<SboxSuperuser.ActorIdentity>)Authenticate, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
	(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( 100, origin: SboxSuperuser.CommandOrigin.Chat ), CancellationToken.None
} );
if ( page.Entries.Count != 7 || page.Entries.Select( row => row.RequestId ).Distinct().Count() != 7
	|| page.Entries.Any( row => row.ActorSubject != sender.SteamId.ToString() || row.ActorKind != SboxSuperuser.ActorKind.Player )
	|| page.Entries.Count( row => row.Command == "version" && row.Outcome == SboxSuperuser.CommandStatus.Success ) != 3
	|| page.Entries.Count( row => row.Command == "help" && row.Outcome == SboxSuperuser.CommandStatus.Success ) != 1
	|| page.Entries.Count( row => row.Command == "group.create" && row.Outcome == SboxSuperuser.CommandStatus.Denied ) != 1
	|| page.Entries.Count( row => row.Command == "command.unresolved" && row.Outcome == SboxSuperuser.CommandStatus.NotFound ) != 1
	|| page.Entries.Count( row => row.Command == "command.invalid" && row.Outcome == SboxSuperuser.CommandStatus.InvalidArguments ) != 1 )
	throw new InvalidOperationException( "Unexpected chat audit count, attribution or outcome." );
return new { Pass = true, sender.Id, sender.SteamId, Rows = page.Entries.Select( row => new { row.RequestId, row.Command, row.Outcome } ).ToArray() };
