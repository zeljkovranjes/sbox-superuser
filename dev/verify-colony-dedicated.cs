const string target = "76561198000000071";
const string command = "colony.supply";
const int restoredValue = 50;
var adapter = Game.ActiveScene.GetAllComponents<SuperuserColony.Components.ColonyAdministration>().Single();
var roster = Game.ActiveScene.GetSystem<SuperuserColony.Systems.ColonyRoster>();
roster.Join( target );
int ReadGameplayValue() => roster.Supplies( target ).Value;
if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Actual dedicated sample host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
Require( runtime is not null, "No dedicated runtime." );
var audit = runtime.GetType().GetProperty( "Audit", flags ).GetValue( runtime );
SboxSuperuser.ActorIdentity Authenticate()
{
	var values = new object[] { null };
	return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, values ) ? (SboxSuperuser.ActorIdentity)values[ 0 ] : null;
}
async Task<SboxSuperuser.AuditPage> Query( string name ) => await (Task<SboxSuperuser.AuditPage>)audit.GetType().GetMethod( "QueryAsync", flags ).Invoke( audit, new object[]
{
	(Func<SboxSuperuser.ActorIdentity>)Authenticate, (Func<SboxSuperuser.ActorIdentity, bool>)(_ => true),
	(Func<SboxSuperuser.ActorIdentity, bool>)(_ => false), new SboxSuperuser.AuditQuery( command: name ), cancellationToken
} );
async Task Native( string name, SboxSuperuser.CommandStatus expected, params string[] args )
{
	var known = (await Query( name )).Entries.Select( item => item.Id ).ToHashSet();
	ConsoleSystem.Run( "admin", new object[] { name }.Concat( args.Cast<object>() ).ToArray() );
	for ( var i = 0; i < 100; i++ )
	{
		var row = (await Query( name )).Entries.FirstOrDefault( item => !known.Contains( item.Id ) && item.CompletedUtc.HasValue );
		if ( row is not null )
		{
			Require( row.Outcome == expected && row.ActorKind == SboxSuperuser.ActorKind.DedicatedConsole, "Native outcome/actor mismatch: " + name + " " + row.Outcome );
			return;
		}
		await Task.Delay( 25, cancellationToken );
	}
	throw new InvalidOperationException( "Native command did not complete: " + name );
}
await Native( command, SboxSuperuser.CommandStatus.Success, target );
Require( ReadGameplayValue() == restoredValue, "Actual game state did not change." );
adapter.Enabled = false;
adapter.ProvideGameplayCapability = false;
adapter.Enabled = true;
await Task.Delay( 100, cancellationToken );
await Native( command, SboxSuperuser.CommandStatus.Unsupported, target );
adapter.Enabled = false;
adapter.ProvideGameplayCapability = true;
adapter.Enabled = true;
await Task.Delay( 100, cancellationToken );
await Native( "group.create", SboxSuperuser.CommandStatus.Success, "portability" );
await Native( "group.permission.allow", SboxSuperuser.CommandStatus.Success, "portability", command );
await Native( "user.group.add", SboxSuperuser.CommandStatus.Success, target, "portability" );
await Native( "warn", SboxSuperuser.CommandStatus.Success, target, "Dedicated portability warning" );
await Native( "ban", SboxSuperuser.CommandStatus.Success, target, "1h", "Dedicated portability ban" );
await Native( "mute", SboxSuperuser.CommandStatus.Success, target, "1h", "Dedicated portability mute" );
var stored = await SboxSuperuser.Superuser.GetStore().ReadAsync( new SboxSuperuser.StoreKey( "moderation", "history" ), 1, cancellationToken );
Require( stored.Status == SboxSuperuser.StoreReadStatus.Found && stored.Document.Revision == 3, "Dedicated moderation history missing." );
return new { Pass = true, Dedicated = true, command, Actual = ReadGameplayValue(), MissingCapability = "Unsupported", NativePolicyEdits = 3, ModerationRevision = stored.Document.Revision, OfflineFixture = target };
