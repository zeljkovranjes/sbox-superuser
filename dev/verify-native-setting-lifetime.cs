if ( !Networking.IsHost || !Application.IsDedicatedServer && !Game.IsEditor ) throw new InvalidOperationException( "Isolated host required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() ).OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalEnabled = bootstrap.Enabled;
var originalSwitch = bootstrap.AllowEditorTesting;
var originalCapacity = ConsoleSystem.GetValue( "superuser_example_capacity" );
var originalPending = ConsoleSystem.GetValue( "superuser_example_pending" );
const string ownersPath = "superuser/editor-owners.json";
var originalOwners = FileSystem.Data.FileExists( ownersPath ) ? FileSystem.Data.ReadAllText( ownersPath ) : null;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
try
{
	if ( Game.IsEditor ) FileSystem.Data.WriteAllText( ownersPath, "{\"schemaVersion\":1,\"owners\":[\"" + Connection.Local.SteamId + "\"]}" );
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = Game.IsEditor;
	bootstrap.Enabled = true;
	Require( (await SboxSuperuser.Superuser.RegisterModulesAsync( new[] { SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.CreateModule() }, cancellationToken )).Status == SboxSuperuser.CommandStatus.Success, "Module startup failed." );
	using var service = SboxSuperuser.Superuser.CreateCommandService( "t15.lifetime", new[] { "example.settings" } );
	Task<SboxSuperuser.CommandResult> Run( string command, params string[] args ) => service.ExecuteAsync( new SboxSuperuser.CommandRequest( command, args, Guid.NewGuid() ), cancellationToken );
	ConsoleSystem.SetValue( "superuser_example_capacity", 99 );
	var actual = await Run( "settings.get.example.capacity" );
	Require( actual.Status == SboxSuperuser.CommandStatus.Success && actual.Data[ "active" ] == "99", "Out-of-bounds native state was hidden." );
	Require( (await Run( "settings.reset.example.capacity" )).Status == SboxSuperuser.CommandStatus.Success && ConsoleSystem.GetValue( "superuser_example_capacity" ) == "8", "Out-of-bounds state could not reset." );
	ConsoleSystem.SetValue( "superuser_example_pending", 2 );
	Require( (await Run( "settings.set.example.pending", "6" )).Status == SboxSuperuser.CommandStatus.Success, "Could not stage pending setting." );
	var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
	SboxSuperuser.ActorIdentity Authenticate()
	{
		if ( Game.IsEditor ) return SboxSuperuser.Superuser.TryGetPlayerIdentity( Connection.Local, out var player ) ? player : null;
		var parameters = new object[] { null };
		return (bool)runtime.GetType().GetMethod( "TryResolveDedicatedConsole", flags ).Invoke( runtime, parameters ) ? (SboxSuperuser.ActorIdentity)parameters[ 0 ] : null;
	}
	var modules = runtime.GetType().GetProperty( "Modules", flags ).GetValue( runtime );
	var commandCount = SboxSuperuser.Superuser.GetCommandDescriptors().Count;
	foreach ( var enabled in new[] { false, true } )
	{
		var result = await (Task<SboxSuperuser.CommandResult>)modules.GetType().GetMethod( "SetEnabledAsync", flags ).Invoke( modules,
			new object[] { (Func<SboxSuperuser.ActorIdentity>)Authenticate, "superuser.settings", enabled, cancellationToken } );
		Require( result.Status == SboxSuperuser.CommandStatus.Success, "Module lifecycle failed." );
		if ( !enabled ) Require( (await Run( "settings.get.example.pending" )).Status == SboxSuperuser.CommandStatus.NotFound, "Disabled settings retained entry points." );
	}
	Require( ConsoleSystem.GetValue( "superuser_example_pending" ) == "2" && (await Run( "settings.get.example.pending" )).Data[ "pending" ] == "True", "Module enable applied restart-only value early." );
	Require( SboxSuperuser.Superuser.GetCommandDescriptors().Count == commandCount, "Module enable duplicated native binding descriptors." );
	var chatVerified = false;
	if ( Game.IsEditor )
	{
		var result = await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
			new SboxSuperuser.CommandRequest( "settings.set.example.sensitive", new[] { "t15-secret-sentinel" }, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Chat, cancellationToken );
		Require( result.Status == SboxSuperuser.CommandStatus.Denied && ConsoleSystem.GetValue( "superuser_example_sensitive" ) == "test-placeholder", "Owner bypassed sensitive chat refusal." );
		chatVerified = true;
	}
	return new { Pass = true, Dedicated = Application.IsDedicatedServer, ReloadKeptPending = true, NoDuplicates = true, OwnerChatDenied = chatVerified };
}
finally
{
	bootstrap.Enabled = false;
	if ( Game.IsEditor )
	{
		if ( originalOwners is null ) FileSystem.Data.DeleteFile( ownersPath );
		else FileSystem.Data.WriteAllText( ownersPath, originalOwners );
	}
	ConsoleSystem.SetValue( "superuser_example_capacity", originalCapacity );
	ConsoleSystem.SetValue( "superuser_example_pending", originalPending );
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = originalEnabled;
}
