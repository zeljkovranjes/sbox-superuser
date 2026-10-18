if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Actual dedicated fixture required." );
if ( FileSystem.Data.FileExists( "superuser-t16/restart-proof.json" ) ) throw new InvalidOperationException( "Restart proof already exists; inspect before seeding." );
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
if ( runtime is null ) throw new InvalidOperationException( "Dedicated bootstrap is not ready." );
var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
SboxSuperuser.ActorIdentity Authenticate()
{
	var parameters = new object[] { null };
	return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, parameters ) ? (SboxSuperuser.ActorIdentity)parameters[ 0 ] : null;
}
async Task<SboxSuperuser.AuditPage> Query( string command ) => await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
{
	(Func<SboxSuperuser.ActorIdentity>)Authenticate, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
	(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( command: command ), CancellationToken.None
} );
async Task Run( string command, params string[] args )
{
	var known = (await Query( command )).Entries.Select( entry => entry.Id ).ToHashSet();
	ConsoleSystem.Run( "admin", new object[] { command }.Concat( args.Cast<object>() ).ToArray() );
	for ( var attempt = 0; attempt < 100; attempt++ )
	{
		var entry = (await Query( command )).Entries.FirstOrDefault( item => !known.Contains( item.Id ) && item.CompletedUtc.HasValue );
		if ( entry is not null )
		{
			if ( entry.Outcome != SboxSuperuser.CommandStatus.Success || entry.ActorKind != SboxSuperuser.ActorKind.DedicatedConsole )
				throw new InvalidOperationException( "Native moderation failed: " + entry.Outcome );
			return;
		}
		await Task.Delay( 25, cancellationToken );
	}
	throw new InvalidOperationException( "No native moderation completion observed." );
}
await Run( "ban", "76561198000000050", "permanent", "T16 permanent restart ban" );
await Run( "ban", "76561198000000051", "1h", "T16 UTC restart ban" );
await Run( "mute", "76561198000000050", "permanent", "T16 restart mute" );
var stored = await SboxSuperuser.Superuser.GetStore().ReadAsync( new SboxSuperuser.StoreKey( "moderation", "history" ), 1, cancellationToken );
if ( stored.Status != SboxSuperuser.StoreReadStatus.Found ) throw new InvalidOperationException( "Dedicated history missing." );
FileSystem.Data.CreateDirectory( "superuser-t16" );
FileSystem.Data.WriteAllText( "superuser-t16/restart-proof.json", stored.Document.PayloadJson );
return new { Pass = true, Dedicated = true, stored.Document.Revision, Records = 3, NativeConsole = true };
