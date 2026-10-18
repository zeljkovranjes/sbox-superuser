// Trusted test script for the isolated editor host or actual dedicated fixture.
if ( !Networking.IsHost || !Networking.IsActive || !Application.IsDedicatedServer && !Game.IsEditor )
	throw new InvalidOperationException( "Eligible isolated host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() )
	.OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalSwitch = bootstrap.AllowEditorTesting;
var originalEnabled = bootstrap.Enabled;
var savedOriginal = ConsoleSystem.GetValue( "superuser_example_saved", null );
var capacityOriginal = ConsoleSystem.GetValue( "superuser_example_capacity", null );
var pendingOriginal = ConsoleSystem.GetValue( "superuser_example_pending", null );
var clampedOriginal = ConsoleSystem.GetValue( "superuser_example_clamp", null );
SboxSuperuser.CommandService service = null;
async Task Start()
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = Game.IsEditor;
	bootstrap.Enabled = true;
	var module = SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.CreateModule();
	var result = await SboxSuperuser.Superuser.RegisterModulesAsync( new[] { module }, cancellationToken );
	Require( result.Status == SboxSuperuser.CommandStatus.Success, "Native binding module startup failed: " + result.Message );
	service = SboxSuperuser.Superuser.CreateCommandService( "t15.fixture", new[] { "example.settings", "admin.help" } );
}
async Task<SboxSuperuser.CommandResult> Run( string command, params string[] args ) => await service.ExecuteAsync(
	new SboxSuperuser.CommandRequest( command, args, Guid.NewGuid() ), cancellationToken );
async Task<SboxSuperuser.StoreReadResult> Stored( string key ) => await SboxSuperuser.Superuser.GetStore().ReadAsync(
	new SboxSuperuser.StoreKey( "superuser.settings", "example." + key ), 1, cancellationToken );
try
{
	await Start();
	Require( (await Run( "settings.set.example.capacity", "12" )).Status == SboxSuperuser.CommandStatus.Success, "Framework setting write failed." );
	var first = await Stored( "capacity" );
	Require( first.Status == SboxSuperuser.StoreReadStatus.Found && ConsoleSystem.GetValue( "superuser_example_capacity" ) == "12", "Confirmed write missing from store or native readback." );
	Require( (await Run( "settings.set.example.capacity", "99" )).Status == SboxSuperuser.CommandStatus.InvalidArguments, "Out-of-range setting accepted." );
	Require( (await Stored( "capacity" )).Document.Revision == first.Document.Revision, "Failed validation changed desired state." );
	foreach ( var key in new[] { "protected", "admin", "cheat", "missing" } )
		Require( (await Run( "settings.set.example." + key, "8" )).Status == SboxSuperuser.CommandStatus.Unsupported, "Inaccessible binding accepted: " + key );
	ConsoleSystem.SetValue( "superuser_example_clamp", 3 );
	var clampBefore = await Stored( "clamp" );
	var clamp = await Run( "settings.set.example.clamp", "12" );
	Require( clamp.Status == SboxSuperuser.CommandStatus.Failed, "Native clamp mismatch did not reject write: " + clamp.Status );
	Require( ConsoleSystem.GetValue( "superuser_example_clamp" ) == "3", "Failed native write did not restore previous value." );
	Require( (await Stored( "clamp" )).Document?.Revision == clampBefore.Document?.Revision, "Failed native write changed stored state." );
	Require( (await Run( "settings.set.example.saved", "9" )).Status == SboxSuperuser.CommandStatus.Success, "Engine-owned setting failed." );
	Require( (await Stored( "saved" )).Status == SboxSuperuser.StoreReadStatus.Missing, "Framework competed with engine persistence." );
	Require( (await Run( "settings.reset.example.saved" )).Status == SboxSuperuser.CommandStatus.Success && ConsoleSystem.GetValue( "superuser_example_saved" ) == "4", "Explicit reset failed." );
	ConsoleSystem.SetValue( "superuser_example_pending", 2 );
	Require( (await Run( "settings.set.example.pending", "6" )).Status == SboxSuperuser.CommandStatus.Success, "Pending setting failed." );
	var pending = await Run( "settings.get.example.pending" );
	Require( pending.Data[ "active" ] == "2" && pending.Data[ "desired" ] == "6" && pending.Data[ "pending" ] == "True", "Pending and active were conflated." );
	var beforeCount = SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.Executions;
	var request = new SboxSuperuser.CommandRequest( "native.example.increment", new[] { "quoted text; still one argument" }, Guid.NewGuid() );
	Require( (await service.ExecuteAsync( request, cancellationToken )).Status == SboxSuperuser.CommandStatus.Unknown, "Native command fabricated a success acknowledgment." );
	await service.ExecuteAsync( request, cancellationToken );
	Require( SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.Executions == beforeCount + 1
		&& SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.LastArgument == "quoted text; still one argument", "Native quoting or retry deduplication failed." );
	var secret = await Run( "settings.get.example.sensitive" );
	Require( secret.Status == SboxSuperuser.CommandStatus.Success && secret.Data[ "active" ] == "[redacted]", "Sensitive value leaked." );
	service.Dispose();
	ConsoleSystem.SetValue( "superuser_example_capacity", 1 );
	await Start();
	Require( ConsoleSystem.GetValue( "superuser_example_capacity" ) == "12" && ConsoleSystem.GetValue( "superuser_example_pending" ) == "6", "New runtime did not reapply desired settings." );
	Require( (await Run( "settings.get.example.pending" )).Data[ "pending" ] == "False", "Applied desired value still shown pending." );
	var listing = await Run( "settings.list" );
	Require( listing.Status == SboxSuperuser.CommandStatus.Success && listing.Data.Count <= 10, "Settings listing failed or exceeded page bounds." );
	return new { Pass = true, Dedicated = Application.IsDedicatedServer,
		NativeExecutions = SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.Executions - beforeCount,
		Capacity = ConsoleSystem.GetValue( "superuser_example_capacity" ), Pending = ConsoleSystem.GetValue( "superuser_example_pending" ),
		ClampOutcome = clamp.Status, SavedFrameworkState = (await Stored( "saved" )).Status };
}
finally
{
	service?.Dispose();
	bootstrap.Enabled = false;
	ConsoleSystem.SetValue( "superuser_example_saved", savedOriginal );
	ConsoleSystem.SetValue( "superuser_example_capacity", capacityOriginal );
	ConsoleSystem.SetValue( "superuser_example_pending", pendingOriginal );
	ConsoleSystem.SetValue( "superuser_example_clamp", clampedOriginal );
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = originalEnabled;
}
