if ( !Game.IsEditor || !Networking.IsHost || !SboxSuperuser.Superuser.IsOwner( Connection.Local ) ) throw new InvalidOperationException( "Isolated editor owner required." );
const string proofPath = "superuser-t16/editor-recovery-proof.json";
if ( FileSystem.Data.FileExists( proofPath ) ) throw new InvalidOperationException( "Recovery proof exists; inspect before injecting." );
var store = (SboxSuperuser.IRecoverableAdminStore)SboxSuperuser.Superuser.GetStore();
var key = new SboxSuperuser.StoreKey( "moderation", "history" );
var prior = await store.ReadAsync( key, 1, cancellationToken );
var backups = await store.GetBackupsAsync( key, cancellationToken );
var backup = backups.Backups.Single( item => item.Revision == prior.Document.Revision );
var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.Player,
	Connection.Local.SteamId.ToString(), SboxSuperuser.CommandOrigin.Console, "moderation.failure.fixture", DateTimeOffset.UtcNow );
FileSystem.Data.CreateDirectory( "superuser-t16" );
FileSystem.Data.WriteAllText( proofPath, Json.Serialize( new { backup.GenerationId, PriorRevision = prior.Document.Revision, Payload = prior.Document.PayloadJson } ) );
var result = await store.WriteAsync( new SboxSuperuser.PolicyTransaction( "{\"schema\":99,\"entries\":[]}", new[] { intent } ).CreateWriteRequest( key, prior.Document.Revision ), cancellationToken );
if ( result.Status != SboxSuperuser.StoreWriteStatus.Committed ) throw new InvalidOperationException( "Failure fixture was not committed." );
for ( var attempt = 0; SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.ModerationDegraded && attempt < 100; attempt++ ) await Task.Delay( 25, cancellationToken );
var player = Connection.All.Single( connection => connection.SteamId == 90071996842377217UL );
if ( SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.ModerationDegraded
	|| SboxSuperuser.Superuser.CheckOrdinaryChat( player ).Status != SboxSuperuser.CommandStatus.StorageUnavailable )
	throw new InvalidOperationException( "Unsupported moderation schema did not fail closed." );
return new { Pass = true, backup.GenerationId, prior.Document.Revision, CurrentRevision = result.CommittedRevision, Health = SboxSuperuser.Superuser.GetDiagnostics() };
