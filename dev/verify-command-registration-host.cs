if ( !Game.IsEditor || !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Editor host required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalSwitch = bootstrap.AllowEditorTesting;
SboxSuperuser.CommandRegistrationScope first = null;
SboxSuperuser.CommandRegistrationScope second = null;
void Require( bool pass, string message )
{
	if ( !pass ) throw new InvalidOperationException( message );
}
try
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = true;
	bootstrap.Enabled = true;
	first = SboxSuperuser.Superuser.BeginCommandRegistration( "sample.dogs" );
	first.RegisterPermission( new SboxSuperuser.PermissionDescriptor( "dog.spawn", "Spawn game-owned dogs" ) );
	first.DiscoverCommands();
	first.Commit();
	Require( SboxSuperuser.Superuser.GetCommandDescriptors().Count == 2, "Host catalog missing discovered commands." );
	var explanation = await SboxSuperuser.Superuser.ExplainPermissionAsync( Connection.Local, Connection.Local.SteamId.ToString(), "dog.spawn" );
	Require( explanation is not null && explanation.IsRegistered && !explanation.IsAllowed, "Registration must add a known key without granting it." );
	second = SboxSuperuser.Superuser.BeginCommandRegistration( "sample.dogs" );
	second.RegisterPermission( new SboxSuperuser.PermissionDescriptor( "dog.spawn", "Spawn game-owned dogs" ) );
	second.RegisterCommand( new SboxSuperuser.CommandDescriptor( "dog.explicit", "Explicit fallback", "dog.spawn", "sample.dogs" ),
		( _, _ ) => Task.FromResult( new SboxSuperuser.CommandResult( SboxSuperuser.CommandStatus.Unsupported, "No game capability" ) ) );
	second.Commit();
	first.Dispose();
	Require( SboxSuperuser.Superuser.GetCommandDescriptors().Single().Name == "dog.explicit", "Explicit replacement or old-scope revocation failed." );
	second.Dispose();
	explanation = await SboxSuperuser.Superuser.ExplainPermissionAsync( Connection.Local, Connection.Local.SteamId.ToString(), "dog.spawn" );
	Require( explanation is not null && !explanation.IsRegistered, "Disposal did not invalidate permission registration." );
	Require( SboxSuperuser.Superuser.GetCommandDescriptors().Count == 0, "Disposal retained a command." );
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = false;
	bootstrap.Enabled = true;
	var refused = false;
	try { SboxSuperuser.Superuser.BeginCommandRegistration( "disabled" ); }
	catch ( InvalidOperationException ) { refused = true; }
	Require( refused, "Editor default-off guard accepted registration." );
	Log.Info( "[T07] host registration PASS: opt-in guard, discovery, explicit replacement, permissions and disposal" );
	return new { Pass = true, CommandsAfterDispose = SboxSuperuser.Superuser.GetCommandDescriptors().Count };
}
finally
{
	first?.Dispose();
	second?.Dispose();
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = true;
}
