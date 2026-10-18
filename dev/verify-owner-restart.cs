// Read-only verification after restarting the dedicated console fixture.
if ( !Application.IsDedicatedServer ) throw new InvalidOperationException( "Dedicated server required." );
var health = SboxSuperuser.Superuser.GetAuthorityDiagnostics();
if ( health.OwnerRevision != 6 || health.OwnerCount != 2 || health.OwnerSource != "managed" || health.OwnerStatus != SboxSuperuser.StoreReadStatus.Found )
	throw new InvalidOperationException( "Managed owners did not survive server restart." );
var read = await SboxSuperuser.Superuser.GetStore().ReadAsync( new SboxSuperuser.StoreKey( "security", "owners" ), 1, System.Threading.CancellationToken.None );
if ( !read.Document.PayloadJson.Contains( "76561198000000001" ) || !read.Document.PayloadJson.Contains( "76561198000000002" ) )
	throw new InvalidOperationException( "Managed owner IDs changed." );
Log.Info( "[T06] managed owner restart PASS: revision=6; count=2; bootstrap configuration unchanged" );
return health;
