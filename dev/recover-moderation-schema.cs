if ( !Game.IsEditor || !Networking.IsHost || !SboxSuperuser.Superuser.IsOwner( Connection.Local ) ) throw new InvalidOperationException( "Isolated editor owner required." );
using var proof = System.Text.Json.JsonDocument.Parse( FileSystem.Data.ReadAllText( "superuser-t16/editor-recovery-proof.json" ) );
var generation = proof.RootElement.GetProperty( "GenerationId" ).GetGuid();
var priorRevision = proof.RootElement.GetProperty( "PriorRevision" ).GetInt64();
var store = (SboxSuperuser.IRecoverableAdminStore)SboxSuperuser.Superuser.GetStore();
var key = new SboxSuperuser.StoreKey( "moderation", "history" );
var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.Player,
	Connection.Local.SteamId.ToString(), SboxSuperuser.CommandOrigin.Console, "moderation.recovery.fixture", DateTimeOffset.UtcNow );
var restored = await store.RecoverAsync( key, generation, priorRevision + 1, intent, cancellationToken );
if ( restored.Status != SboxSuperuser.StoreWriteStatus.Committed ) throw new InvalidOperationException( "Recovery failed: " + restored.Status );
for ( var attempt = 0; SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.Ready && attempt < 100; attempt++ ) await Task.Delay( 25, cancellationToken );
var current = await store.ReadAsync( key, 1, cancellationToken );
using var original = System.Text.Json.JsonDocument.Parse( proof.RootElement.GetProperty( "Payload" ).GetString() );
using var recovered = System.Text.Json.JsonDocument.Parse( current.Document.PayloadJson );
if ( original.RootElement.GetProperty( "state" ).GetRawText() != recovered.RootElement.GetProperty( "state" ).GetRawText()
	|| SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.Ready )
	throw new InvalidOperationException( "Recovery changed history or left moderation unavailable." );
return new { Pass = true, Revision = current.Document.Revision, HistoryPreserved = true, Health = SboxSuperuser.Superuser.GetDiagnostics() };
