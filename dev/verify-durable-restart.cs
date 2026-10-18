// Read-only restart inspection for the dedicated interruption fixture.
if ( !Application.IsDedicatedServer ) throw new InvalidOperationException( "Dedicated server required." );
var store = SboxSuperuser.Superuser.GetStore() ?? throw new InvalidOperationException( "Bootstrap unavailable." );
var key = new SboxSuperuser.StoreKey( "t04.restart", "records" );
var read = await store.ReadAsync( key, 1, System.Threading.CancellationToken.None );
if ( read.Status == SboxSuperuser.StoreReadStatus.Found )
{
	if ( !read.Document.PayloadJson.Contains( "T04-survive-process-restart" )
		|| !read.Document.PayloadJson.Contains( "76561198000000001" ) || !read.Document.PayloadJson.Contains( "76561198000000002" ) )
		throw new InvalidOperationException( "Acknowledged baseline state changed or disappeared." );
}
else if ( read.Status != SboxSuperuser.StoreReadStatus.Corrupt )
{
	throw new InvalidOperationException( "Unexpected restart state: " + read.Status );
}
var backups = await store.GetBackupsAsync( key, System.Threading.CancellationToken.None );
if ( !backups.Backups.Any( item => item.Revision == 1 ) ) throw new InvalidOperationException( "No recoverable acknowledged baseline." );
Log.Info( $"[T04] restart inspection; status={read.Status}; revision={read.Document?.Revision}; head={backups.HeadRevision}; validBackups={backups.Backups.Count}" );
return new { Read = read, Backups = backups };
