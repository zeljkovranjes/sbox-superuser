if ( !Networking.IsHost || !Game.IsEditor ) throw new InvalidOperationException( "Isolated editor host required." );
if ( FileSystem.Data.FileExists( "superuser/editor-owners.json" ) ) throw new InvalidOperationException( "This isolated fixture expects no existing owner file; inspect before setup." );
if ( SboxSuperuser.Superuser.GetDiagnostics().RuntimeId.HasValue ) throw new InvalidOperationException( "Stop the previous test runtime before setup." );
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() ).OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
FileSystem.Data.WriteAllText( "superuser/editor-owners.json", "{\"schemaVersion\":1,\"owners\":[\"" + Connection.Local.SteamId + "\"]}" );
bootstrap.Enabled = false;
bootstrap.AllowEditorTesting = true;
bootstrap.Enabled = true;
for ( var attempt = 0; SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.Ready && attempt < 100; attempt++ )
	await Task.Delay( 25, cancellationToken );
if ( SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.Ready ) throw new InvalidOperationException( "Moderation runtime did not become ready." );
var backend = new GameObject( true, "T16 Independent Chat Backend" );
backend.Components.Create<SuperuserCompatibilityLab.Components.IndependentChatExample>();
backend.NetworkSpawn();
return new { Pass = true, backend.Id, Health = SboxSuperuser.Superuser.GetDiagnostics(), OriginalOwnerFile = "absent", OriginalEditorSwitch = false };
