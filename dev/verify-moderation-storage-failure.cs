if ( !Game.IsEditor || !Networking.IsHost || !SboxSuperuser.Superuser.IsOwner( Connection.Local ) ) throw new InvalidOperationException( "Isolated editor owner required." );
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var store = SboxSuperuser.Superuser.GetStore();
var coordinator = store.GetType().GetField( "_coordinator", flags ).GetValue( store );
var files = coordinator.GetType().GetField( "_files", flags ).GetValue( coordinator );
var checkpoint = files.GetType().GetProperty( "WriteCheckpoint", flags );
var key = new SboxSuperuser.StoreKey( "moderation", "history" );
var before = await store.ReadAsync( key, 1, cancellationToken );
try
{
	checkpoint.SetValue( files, (Func<string, CancellationToken, Task>)((path, _) => path.Contains( "/moderation/history/g-" )
		? Task.FromException( new System.IO.IOException( "Injected moderation persistence failure" ) ) : Task.CompletedTask) );
	var result = await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
		new SboxSuperuser.CommandRequest( "ban", new[] { "90071996842377217", "1h", "Must fail before disconnect" }, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
	checkpoint.SetValue( files, null );
	if ( result.Status != SboxSuperuser.CommandStatus.StorageUnavailable
		|| !Connection.All.Any( connection => connection.IsActive && connection.SteamId == 90071996842377217UL )
		|| (await store.ReadAsync( key, 1, cancellationToken )).Document.Revision != before.Document.Revision )
		throw new InvalidOperationException( "Failed ban changed committed state, disconnected the target or falsely succeeded." );
	return new { Pass = true, result.Status, PriorRevision = before.Document.Revision, TargetStillConnected = true };
}
finally
{
	checkpoint.SetValue( files, null );
}
