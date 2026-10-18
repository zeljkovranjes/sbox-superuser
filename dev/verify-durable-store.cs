// Trusted development script, isolated adopting game only. Leaves uniquely named test records for inspection.
if ( !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Host session required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalOptIn = bootstrap.AllowEditorTesting;
if ( Game.IsEditor )
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = true;
	bootstrap.Enabled = true;
}

void Require( bool value, string detail )
{
	if ( !value ) throw new InvalidOperationException( detail );
}

SboxSuperuser.AuditIntent Intent() => new( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.InternalService,
	"test.storage", SboxSuperuser.CommandOrigin.InternalService, "test.change", DateTimeOffset.UtcNow );
var key = new SboxSuperuser.StoreKey( "t04.engine", "run" + Guid.NewGuid().ToString( "N" ) );
var mode = Application.IsDedicatedServer ? "dedicated" : "editor";
var ownerPath = Application.IsDedicatedServer ? "superuser/owners.json" : "superuser/editor-owners.json";
var ownersBefore = FileSystem.Data.FileExists( ownerPath ) ? FileSystem.Data.ReadAllText( ownerPath ) : null;
var store = SboxSuperuser.Superuser.GetStore();
Require( store is not null, "Eligible bootstrap did not expose storage." );
var token = System.Threading.CancellationToken.None;
var transaction = new SboxSuperuser.PolicyTransaction( "{\"owners\":[\"76561198000000001\"],\"bans\":[\"76561198000000002\"]}", new[] { Intent() } );
Require( (await store.ReadAsync( key, 1, token )).Status == SboxSuperuser.StoreReadStatus.Missing, "Fresh key was not Missing." );
Require( (await store.WriteAsync( transaction.CreateWriteRequest( key, 0 ), token )).Status == SboxSuperuser.StoreWriteStatus.Committed, "Initial write failed." );
var initial = await store.ReadAsync( key, 1, token );
Require( initial.Document.Revision == 1, "Initial readback failed." );
Require( (await store.WriteAsync( transaction.CreateWriteRequest( key, 0 ), token )).Status == SboxSuperuser.StoreWriteStatus.Conflict, "Stale write accepted." );
Require( (await store.WriteAsync( transaction.CreateWriteRequest( key, 1 ), token )).Status == SboxSuperuser.StoreWriteStatus.Committed, "Second write failed." );
var backups = await store.GetBackupsAsync( key, token );
Require( backups.Backups.Count == 2, "File enumeration did not discover both backups." );
var latest = backups.Backups[ 0 ];
var corruptPath = $"superuser/store/{mode}/{key.ModuleId}/{key.DocumentId}/g-{latest.Revision:D20}-{latest.GenerationId:N}.json";
FileSystem.Data.WriteAllText( corruptPath, "{interrupted" );
Require( (await store.ReadAsync( key, 1, token )).Status == SboxSuperuser.StoreReadStatus.Corrupt, "Damaged commit did not fail closed." );
Require( (await store.WriteAsync( transaction.CreateWriteRequest( key, 0 ), token )).Status == SboxSuperuser.StoreWriteStatus.Corrupt, "Corruption was treated as first installation." );
backups = await store.GetBackupsAsync( key, token );
Require( backups.Status == SboxSuperuser.StoreReadStatus.Corrupt && backups.Backups.Count == 1, "Valid backup not available during corruption." );
var recovery = await store.RecoverAsync( key, backups.Backups[ 0 ].GenerationId, backups.HeadRevision, Intent(), token );
Require( recovery.Status == SboxSuperuser.StoreWriteStatus.Committed && recovery.CommittedRevision == 3, "Explicit recovery failed." );
Require( FileSystem.Data.ReadAllText( corruptPath ) == "{interrupted", "Recovery destroyed corrupt evidence." );
var recovered = await store.ReadAsync( key, 1, token );
Require( recovered.Document.PayloadJson.Contains( "76561198000000001" ) && recovered.Document.PayloadJson.Contains( "76561198000000002" ), "Recovery erased owner/ban test records." );
bootstrap.Enabled = false;
Require( (await store.ReadAsync( key, 1, token )).Status == SboxSuperuser.StoreReadStatus.Unavailable, "Retired store remained usable." );
bootstrap.Enabled = true;
store = SboxSuperuser.Superuser.GetStore();
Require( (await store.ReadAsync( key, 1, token )).Document.Revision == 3, "Bootstrap replacement lost records." );
Require( (FileSystem.Data.FileExists( ownerPath ) ? FileSystem.Data.ReadAllText( ownerPath ) : null) == ownersBefore, "Bootstrap owner configuration changed." );
if ( Game.IsEditor )
{
	ConsoleSystem.Run( "superuser_store_backups", key.ModuleId, key.DocumentId );
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = originalOptIn;
	bootstrap.Enabled = true;
	if ( !originalOptIn ) Require( SboxSuperuser.Superuser.GetStore() is null, "Editor switch off still exposed store." );
}
Log.Info( $"[T04] engine storage PASS; mode={mode}; key={key.ModuleId}/{key.DocumentId}; recovered=3; corruption preserved; retired handles refused" );
return new { Pass = true, Mode = mode, Key = key.DocumentId };
