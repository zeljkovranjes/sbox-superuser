// Run in the isolated compatibility lab with the three current tutorial fixtures compiled.
if ( !Game.IsEditor || !Game.IsPlaying || !Networking.IsHost ) throw new InvalidOperationException( "Isolated editor host required." );
void Require( bool condition, string message ) { if ( !condition ) throw new InvalidOperationException( message ); }
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
Require( !FileSystem.Data.FileExists( "superuser/editor-owners.json" ), "Preserve the existing owner file; use an empty isolated lab." );
bootstrap.Enabled = false;
bootstrap.AllowEditorTesting = true;
var factoryCalls = 0;
SboxSuperuser.Superuser.ConfigureStorage( context =>
{
	Require( !context.IsDedicatedServer, "Editor misclassified as dedicated." );
	factoryCalls++;
	return new SuperuserCompatibilityLab.Engine.CustomAdminStore( context.LocalStore );
} );
bootstrap.Enabled = true;
var go = new GameObject( Game.ActiveScene ) { Name = "Admin UI verification" };
var observer = go.Components.Create<SuperuserCompatibilityLab.Components.ReplacementAdminExample>();
var menu = go.Components.Create<SboxSuperuser.Components.SuperuserAdminMenu>();
async Task<SboxSuperuser.AdminReply> Reply()
{
	var id = observer.RequestId;
	for ( var attempt = 0; attempt < 200; attempt++ )
	{
		if ( observer.LastReply?.RequestId == id ) return observer.LastReply;
		await GameTask.DelaySeconds( 0.05f );
	}
	throw new TimeoutException( "Admin reply did not arrive." );
}
observer.Search( "" );
Require( (await Reply()).Status == SboxSuperuser.CommandStatus.Denied, "Non-staff directory access was not denied." );
observer.ConfirmedAction( new SboxSuperuser.AdminActionRequest( SboxSuperuser.AdminAction.Warn, "76561198000000044", "Unauthorized attempt" ) );
Require( (await Reply()).Status == SboxSuperuser.CommandStatus.Denied, "Non-staff warning was not denied." );
bootstrap.Enabled = false;
FileSystem.Data.WriteAllText( "superuser/editor-owners.json", System.Text.Json.JsonSerializer.Serialize( new { schemaVersion = 1, owners = new[] { Connection.Local.SteamId.ToString() } } ) );
bootstrap.Enabled = true;
Require( factoryCalls == 2, "Storage factory must run once per eligible runtime." );
observer.Search( "" );
Require( (await Reply()).Status == SboxSuperuser.CommandStatus.Success, "Owner directory failed." );
const string target = "76561198000000044";
observer.ConfirmedAction( new SboxSuperuser.AdminActionRequest( SboxSuperuser.AdminAction.Warn, target, "Please follow the server rules." ) );
Require( (await Reply()).Status == SboxSuperuser.CommandStatus.Success, "Warning failed." );
observer.ConfirmedAction( new SboxSuperuser.AdminActionRequest( SboxSuperuser.AdminAction.Ban, target, "Repeated disruption after a warning.", "2h" ) );
Require( (await Reply()).Status == SboxSuperuser.CommandStatus.Success, "Offline ban failed." );
observer.Inspect( target );
var detail = await Reply();
Require( detail.Online == false && detail.Restrictions.Length == 1 && detail.History.Length == 2, "Offline details/history mismatch." );
Require( !detail.Actions.Contains( SboxSuperuser.AdminAction.Kick ), "Offline kick was offered." );
observer.Activity();
Require( (await Reply()).Activity.Any( row => row.Command == "ban" && row.Outcome == SboxSuperuser.CommandStatus.Success ), "Audit outcome missing." );
menu.SetOpen( true );
await GameTask.DelaySeconds( 0.2f );
Require( menu.IsOpen && Mouse.Visibility == MouseVisibility.Visible, "Menu did not open/capture cursor." );
menu.Enabled = false;
Require( !menu.IsOpen, "Disabled menu stayed open." );
await GameTask.DelaySeconds( 2.1f );
observer.Inspect( target );
Require( (await Reply()).Status == SboxSuperuser.CommandStatus.Success, "Replacement view stopped when default UI was disabled." );
var console = await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
	new SboxSuperuser.CommandRequest( "help", Array.Empty<string>(), Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console );
Require( console.Status == SboxSuperuser.CommandStatus.Success, "Console failed with default UI disabled." );
Require( SboxSuperuser.Superuser.TryHandleChat( Connection.Local, "/admin help", Guid.NewGuid() ), "Slash chat failed with default UI disabled." );
menu.Enabled = true;
menu.SetOpen( true );
return new { Pass = true, FactoryCalls = factoryCalls, Checks = "denied reads/actions, owner reads/actions, offline ban/history, audit, disable/replacement, console/chat", Target = target };
