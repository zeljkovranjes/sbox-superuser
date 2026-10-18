// Isolated actual dedicated fixture after verify-owner-console.cs; deliberately corrupts only its newest owner generation.
if ( !Application.IsDedicatedServer ) throw new InvalidOperationException( "Dedicated server required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
const string ownerFile = "superuser/owners.json";
var originalOwnerFile = FileSystem.Data.FileExists( ownerFile ) ? FileSystem.Data.ReadAllText( ownerFile ) : null;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
object Policy()
{
	var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime",
		System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static ).Invoke( null, null );
	return runtime.GetType().GetProperty( "Policy", flags ).GetValue( runtime );
}
async System.Threading.Tasks.Task Drain()
{
	var policy = Policy();
	await (System.Threading.Tasks.Task<SboxSuperuser.CommandResult>)policy.GetType().GetMethod( "ReloadAsync", flags )
		.Invoke( policy, new object[] { System.Threading.CancellationToken.None } );
}
bool IsOwner( string id )
{
	var policy = Policy();
	return (bool)policy.GetType().GetMethod( "IsOwner", flags ).Invoke( policy, new object[] { id } );
}
void Require( bool value, string message )
{
	if ( !value ) throw new InvalidOperationException( message );
}
string damagedPath = null;
string originalGeneration = null;
var recovered = false;
try
{
	Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerRevision == 5, "Expected known disposable owner revision 5." );
	bootstrap.Enabled = false;
	FileSystem.Data.WriteAllText( ownerFile, "{\"schemaVersion\":1,\"owners\":[\"76561198000000003\"]}" );
	bootstrap.Enabled = true;
	await Drain();
	Require( !IsOwner( "76561198000000003" ) && IsOwner( "76561198000000001" ), "Managed owner source did not override the old bootstrap file." );
	var store = SboxSuperuser.Superuser.GetStore();
	var key = new SboxSuperuser.StoreKey( "security", "owners" );
	var backups = await store.GetBackupsAsync( key, System.Threading.CancellationToken.None );
	var newest = backups.Backups.Single( item => item.Revision == 5 );
	var restore = backups.Backups.Single( item => item.Revision == 3 );
	damagedPath = $"superuser/store/dedicated/security/owners/g-{newest.Revision:D20}-{newest.GenerationId:N}.json";
	originalGeneration = FileSystem.Data.ReadAllText( damagedPath );
	FileSystem.Data.WriteAllText( damagedPath, "{damaged-owner-generation" );
	ConsoleSystem.Run( "superuser_policy_reload" );
	await Drain();
	Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerStatus == SboxSuperuser.StoreReadStatus.Corrupt, "Managed corruption was not reported." );
	Require( !IsOwner( "76561198000000003" ) && !IsOwner( "76561198000000001" ), "Corruption fell back to a bootstrap or cached owner." );
	Require( SboxSuperuser.Superuser.GetDiagnostics().State == SboxSuperuser.BootstrapState.AuthorityDegraded, "Bootstrap did not report degraded authority." );
	ConsoleSystem.Run( "superuser_store_recover", "security", "owners", restore.GenerationId.ToString( "N" ), "5" );
	await Drain();
	Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerRevision == 6 && IsOwner( "76561198000000001" )
		&& IsOwner( "76561198000000002" ) && !IsOwner( "76561198000000003" ), "Explicit backup recovery failed to restore managed authority." );
	Require( FileSystem.Data.ReadAllText( damagedPath ) == "{damaged-owner-generation", "Recovery destroyed damaged evidence." );
	recovered = true;
	Log.Info( "[T06] managed owner corruption PASS: no bootstrap/cached fallback; degraded diagnostics; explicit local backup recovery; revision=6" );
	return SboxSuperuser.Superuser.GetAuthorityDiagnostics();
}
finally
{
	bootstrap.Enabled = false;
	if ( !recovered && damagedPath is not null && originalGeneration is not null ) FileSystem.Data.WriteAllText( damagedPath, originalGeneration );
	if ( originalOwnerFile is null ) FileSystem.Data.DeleteFile( ownerFile );
	else FileSystem.Data.WriteAllText( ownerFile, originalOwnerFile );
	bootstrap.Enabled = true;
}
