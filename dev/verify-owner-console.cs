// Actual dedicated-local command test for disposable data. Leaves managed owner revision 5 for restart verification.
if ( !Application.IsDedicatedServer ) throw new InvalidOperationException( "Dedicated server required." );
var store = SboxSuperuser.Superuser.GetStore();
var key = new SboxSuperuser.StoreKey( "security", "owners" );
if ( (await store.ReadAsync( key, 1, System.Threading.CancellationToken.None )).Status != SboxSuperuser.StoreReadStatus.Missing )
	throw new InvalidOperationException( "Refuse to replace existing managed owners." );
void Require( bool value, string message )
{
	if ( !value ) throw new InvalidOperationException( message );
}
// Native entries return before async work finishes. The trusted bridge waits behind their authority operations.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime",
	System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static ).Invoke( null, null );
var policy = runtime.GetType().GetProperty( "Policy", flags ).GetValue( runtime );
async System.Threading.Tasks.Task Drain()
{
	var pending = (System.Threading.Tasks.Task<SboxSuperuser.CommandResult>)policy.GetType().GetMethod( "ReloadAsync", flags )
		.Invoke( policy, new object[] { System.Threading.CancellationToken.None } );
	Require( (await pending).Status == SboxSuperuser.CommandStatus.Success, "Authority reload barrier failed." );
}
ConsoleSystem.Run( "superuser_owner_add", "76561198000000001" );
await Drain();
Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerRevision == 1, "Local owner add did not persist." );
ConsoleSystem.Run( "superuser_owner_remove", "76561198000000001" );
await Drain();
Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerRevision == 1, "Final owner removal should refuse." );
ConsoleSystem.Run( "superuser_owner_recover", "clear" );
await Drain();
Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerCount == 0, "Explicit local clear failed." );
ConsoleSystem.Run( "superuser_owner_recover", "replace", "76561198000000001", "76561198000000002" );
await Drain();
Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerCount == 2, "Explicit local replacement failed." );
ConsoleSystem.Run( "superuser_owner_remove", "76561198000000002" );
await Drain();
Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().OwnerCount == 1, "Non-final removal failed." );
ConsoleSystem.Run( "superuser_owner_add", "76561198000000002" );
await Drain();
var health = SboxSuperuser.Superuser.GetAuthorityDiagnostics();
Require( health.OwnerRevision == 5 && health.OwnerCount == 2 && health.OwnerSource == "managed", "Unexpected final owner state." );
Require( !SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Dedicated SteamId0 became a player owner." );
Log.Info( "[T06] dedicated owner console PASS: add/remove, final-owner refusal, explicit clear/replace, persisted revision=5" );
return health;
