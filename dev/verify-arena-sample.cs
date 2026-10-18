var adapter = Game.ActiveScene.GetAllComponents<SuperuserArena.Components.ArenaAdministration>().Single();
const string command = "arena.heal";
const int initialValue = 25, restoredValue = 100;
int ReadGameplayValue() => Game.ActiveScene.GetAllComponents<SuperuserArena.Components.ArenaPawn>().Single( pawn => pawn.SteamId == Connection.Local.SteamId.ToString() ).Health;
// Run on a fresh isolated sample editor host; this fixture creates disposable owner/policy state.
if ( !Game.IsEditor || !Networking.IsHost ) throw new InvalidOperationException( "Isolated sample editor host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
const string ownersPath = "superuser/editor-owners.json";
Require( !FileSystem.Data.FileExists( ownersPath ), "Existing owner file must be inspected before running this fixture." );
async Task Restart( string owners )
{
	adapter.Enabled = false;
	bootstrap.Enabled = false;
	FileSystem.Data.CreateDirectory( "superuser" );
	FileSystem.Data.WriteAllText( ownersPath, "{\"schemaVersion\":1,\"owners\":" + owners + "}" );
	bootstrap.AllowEditorTesting = true;
	bootstrap.Enabled = true;
	adapter.Enabled = true;
	for ( var i = 0; i < 100 && !SboxSuperuser.Superuser.GetCommandDescriptors().Any( item => item.Name == command ); i++ ) await Task.Delay( 25, cancellationToken );
	Require( SboxSuperuser.Superuser.GetCommandDescriptors().Count( item => item.Name == command ) == 1, "Expected exactly one game command." );
}
Task<SboxSuperuser.CommandResult> Run( string name, params string[] args ) => SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
	new SboxSuperuser.CommandRequest( name, args, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
async Task Native( string name, SboxSuperuser.CommandStatus expected, params string[] args )
{
	var known = (await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( command: name ), cancellationToken )).Entries.Select( row => row.Id ).ToHashSet();
	ConsoleSystem.Run( "admin", new object[] { name }.Concat( args.Cast<object>() ).ToArray() );
	for ( var i = 0; i < 100; i++ )
	{
		var page = await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( command: name ), cancellationToken );
		var row = page.Entries.FirstOrDefault( item => !known.Contains( item.Id ) && item.CompletedUtc.HasValue );
		if ( row is not null )
		{
			Require( row.Outcome == expected && row.ActorKind == SboxSuperuser.ActorKind.Player && row.ActorSubject == Connection.Local.SteamId.ToString(), "Native command outcome or actor mismatch: " + name + " " + row.Outcome );
			return;
		}
		await Task.Delay( 25, cancellationToken );
	}
	throw new InvalidOperationException( "Native command produced no audited completion: " + name );
}
var localId = Connection.Local.SteamId.ToString();
await Restart( "[]" );
Require( (await Run( command, localId )).Status == SboxSuperuser.CommandStatus.Denied, "Default player obtained a game permission." );
await Restart( "[\"" + localId + "\"]" );
Require( ReadGameplayValue() == initialValue, "Initial game representation differs." );
await Native( command, SboxSuperuser.CommandStatus.Success, localId );
Require( ReadGameplayValue() == restoredValue, "Registered capability did not change actual game state." );
Require( (await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( Connection.Local, SboxSuperuser.PolicyChange.SetUserPermission( localId, command, SboxSuperuser.PermissionEffect.Allow ), SboxSuperuser.CommandOrigin.Console, cancellationToken )).Status == SboxSuperuser.CommandStatus.Success, "Custom permission grant failed." );
await Restart( "[]" );
Require( !SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Owner fallback was not removed." );
await Native( command, SboxSuperuser.CommandStatus.Success, localId );
await Restart( "[\"" + localId + "\"]" );
Require( (await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( Connection.Local, SboxSuperuser.PolicyChange.SetUserPermission( localId, command, SboxSuperuser.PermissionEffect.Unset ), SboxSuperuser.CommandOrigin.Console, cancellationToken )).Status == SboxSuperuser.CommandStatus.Success, "Custom permission cleanup failed." );
adapter.Enabled = false;
adapter.ProvideGameplayCapability = false;
adapter.Enabled = true;
await Task.Delay( 100, cancellationToken );
await Native( command, SboxSuperuser.CommandStatus.Unsupported, localId );
adapter.Enabled = false;
adapter.ProvideGameplayCapability = true;
adapter.Enabled = true;
await Task.Delay( 100, cancellationToken );
const string offline = "76561198000000070";
await Native( "warn", SboxSuperuser.CommandStatus.Success, offline, "Portability warning" );
await Native( "ban", SboxSuperuser.CommandStatus.Success, offline, "1h", "Portability ban" );
await Native( "mute", SboxSuperuser.CommandStatus.Success, offline, "1h", "Portability mute" );
await Native( "unban", SboxSuperuser.CommandStatus.Success, offline );
await Native( "unmute", SboxSuperuser.CommandStatus.Success, offline );
var history = await Run( "moderation.history", offline );
Require( history.Status == SboxSuperuser.CommandStatus.Success && history.Data.Keys.Count( key => key.StartsWith( "row" ) ) == 5, "Moderation history differs between games." );
Require( SboxSuperuser.Superuser.GetDiagnostics().State == SboxSuperuser.BootstrapState.Ready, "Sample runtime degraded." );
return new { Pass = true, command, Initial = initialValue, Actual = ReadGameplayValue(), DefaultDenied = true, NormalPlayerGrant = true, MissingCapability = "Unsupported", ModerationRows = 5 };
