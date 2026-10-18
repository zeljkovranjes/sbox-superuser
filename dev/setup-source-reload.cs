// Trusted local automation only. Retains old handles outside the scene to prove revocation.
if ( !Game.IsEditor || !Networking.IsHost ) throw new InvalidOperationException( "Isolated editor host required." );
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() ).OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
if ( FileSystem.Data.FileExists( "superuser/editor-owners.json" ) ) throw new InvalidOperationException( "Inspect existing owner configuration before setup." );
FileSystem.Data.WriteAllText( "superuser/editor-owners.json", "{\"schemaVersion\":1,\"owners\":[\"" + Connection.Local.SteamId + "\"]}" );
bootstrap.Enabled = false;
bootstrap.AllowEditorTesting = true;
bootstrap.Enabled = true;
for ( var i = 0; i < 100 && SboxSuperuser.Superuser.GetDiagnostics().State != SboxSuperuser.BootstrapState.Ready; i++ ) await Task.Delay( 25, cancellationToken );
if ( !SboxSuperuser.Superuser.IsOwner( Connection.Local ) ) throw new InvalidOperationException( "Owner setup failed." );
var pulses = new SuperuserCompatibilityLab.Engine.Modules.SamplePulseSource();
var sample = new SuperuserCompatibilityLab.Engine.Modules.SampleCounterModule( pulses );
var held = new SuperuserCompatibilityLab.Engine.Modules.HeldCommandModule();
var registered = await SboxSuperuser.Superuser.RegisterModulesAsync( new SboxSuperuser.IAdminModule[] { sample, held }, cancellationToken );
if ( registered.Status != SboxSuperuser.CommandStatus.Success ) throw new InvalidOperationException( registered.Message );
var store = SboxSuperuser.Superuser.GetStore();
var ban = await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
	new SboxSuperuser.CommandRequest( "ban", new[] { "76561198000000060", "permanent", "Lifecycle retention fixture" }, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
if ( ban.Status != SboxSuperuser.CommandStatus.Success ) throw new InvalidOperationException( "Ban setup failed: " + ban.Status );
var persisted = await store.ReadAsync( new SboxSuperuser.StoreKey( "moderation", "history" ), 1, cancellationToken );
var pending = SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
	new SboxSuperuser.CommandRequest( "sample.hold", Array.Empty<string>(), Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console );
for ( var i = 0; i < 100 && !held.Entered; i++ ) await Task.Delay( 25, cancellationToken );
if ( !held.Entered || pulses.Listeners != 1 ) throw new InvalidOperationException( "Pending/listener setup failed." );
AppDomain.CurrentDomain.SetData( "superuser.t17", new object[] { pulses, sample, held, store, pending, persisted.Document.PayloadJson, SboxSuperuser.Superuser.GetDiagnostics().RuntimeId } );
FileSystem.Data.CreateDirectory( "superuser-t17" );
FileSystem.Data.WriteAllText( "superuser-t17/reload-proof.json", persisted.Document.PayloadJson );
return new { Pass = true, SboxSuperuser.Superuser.GetDiagnostics().RuntimeId, Pending = held.Entered, pulses.Listeners };
