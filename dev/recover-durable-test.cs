// Deliberate operator recovery for the isolated T04 interruption fixture only.
if ( !Application.IsDedicatedServer ) throw new InvalidOperationException( "Dedicated server required." );
var store = SboxSuperuser.Superuser.GetStore() ?? throw new InvalidOperationException( "Bootstrap unavailable." );
var key = new SboxSuperuser.StoreKey( "t04.restart", "records" );
var token = System.Threading.CancellationToken.None;
var current = await store.ReadAsync( key, 1, token );
if ( current.Status != SboxSuperuser.StoreReadStatus.Corrupt ) throw new InvalidOperationException( "Expected interrupted commit, not " + current.Status );
var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.InternalService,
	"test.storage", SboxSuperuser.CommandOrigin.InternalService, "test.reset", DateTimeOffset.UtcNow );
var reset = new SboxSuperuser.PolicyTransaction( "{}", new[] { intent } ).CreateWriteRequest( key, 0 );
if ( (await store.WriteAsync( reset, token )).Status != SboxSuperuser.StoreWriteStatus.Corrupt )
	throw new InvalidOperationException( "Corruption was accepted as fresh installation." );
var backups = await store.GetBackupsAsync( key, token );
var baseline = backups.Backups.Single( item => item.Revision == 1 );
ConsoleSystem.Run( "superuser_store_backups", key.ModuleId, key.DocumentId );
ConsoleSystem.Run( "superuser_store_recover", key.ModuleId, key.DocumentId, baseline.GenerationId.ToString( "N" ), backups.HeadRevision.ToString() );
var recovered = await store.ReadAsync( key, 1, token );
if ( recovered.Status != SboxSuperuser.StoreReadStatus.Found || recovered.Document.Revision != 3
	|| !recovered.Document.PayloadJson.Contains( "T04-survive-process-restart" )
	|| !recovered.Document.PayloadJson.Contains( "storage.recover" ) )
	throw new InvalidOperationException( "Dedicated-local recovery did not commit expected state and intent." );
Log.Info( "[T04] dedicated-local recovery PASS; corrupt reset refused; revision=3; baseline preserved with recovery intent" );
return recovered;
