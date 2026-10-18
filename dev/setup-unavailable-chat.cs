if ( !Game.IsEditor || !Networking.IsHost ) throw new InvalidOperationException( "Isolated editor host required." );
if ( SboxSuperuser.Superuser.GetDiagnostics().BootstrapCount != 1 ) throw new InvalidOperationException( "Expected one initial bootstrap." );
var duplicate = new GameObject( false, "T19 Duplicate Bootstrap" );
duplicate.Components.Create<SboxSuperuser.Components.SuperuserBootstrap>().AllowEditorTesting = true;
duplicate.Enabled = true;
await Task.Delay( 100, cancellationToken );
var health = SboxSuperuser.Superuser.GetDiagnostics();
if ( health.State != SboxSuperuser.BootstrapState.DuplicateBootstrap || health.RuntimeId.HasValue ) throw new InvalidOperationException( "Duplicate did not suspend authority." );
return new { Pass = true, duplicate.Id, health.State };
