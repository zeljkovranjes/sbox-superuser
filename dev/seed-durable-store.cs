// Trusted test script for the isolated actual dedicated server. Do not run in production.
if ( !Application.IsDedicatedServer ) throw new InvalidOperationException( "Dedicated server required." );
var store = SboxSuperuser.Superuser.GetStore() ?? throw new InvalidOperationException( "Bootstrap unavailable." );
var key = new SboxSuperuser.StoreKey( "t04.restart", "records" );
var token = System.Threading.CancellationToken.None;
var read = await store.ReadAsync( key, 1, token );
if ( read.Status != SboxSuperuser.StoreReadStatus.Missing ) throw new InvalidOperationException( "Test already seeded; inspect existing data, do not reset it." );
var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.DedicatedConsole,
	"dedicated-console", SboxSuperuser.CommandOrigin.Console, "test.seed", DateTimeOffset.UtcNow );
var transaction = new SboxSuperuser.PolicyTransaction( "{\"token\":\"T04-survive-process-restart\",\"owners\":[\"76561198000000001\"],\"bans\":[\"76561198000000002\"]}", new[] { intent } );
var result = await store.WriteAsync( transaction.CreateWriteRequest( key, 0 ), token );
if ( result.Status != SboxSuperuser.StoreWriteStatus.Committed ) throw new InvalidOperationException( result.Status.ToString() );
var backups = await store.GetBackupsAsync( key, token );
Log.Info( $"[T04] dedicated baseline committed; generation={backups.Backups[ 0 ].GenerationId:N}; revision=1" );
return backups;
