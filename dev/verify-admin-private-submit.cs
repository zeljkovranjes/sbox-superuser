// Run only on client A after both clients have prepared the known correlation ID.
if ( Networking.IsHost || Connection.Local.SteamId != 90071996842377217UL ) throw new InvalidOperationException( "Client A required." );
var facade = typeof( SboxSuperuser.Superuser );
var id = Guid.Parse( "082a5560-f11d-4de5-9bd8-8849eeefc261" );
facade.GetMethod( "ForwardAdminQuery", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic )
	.Invoke( null, new object[] { id, SboxSuperuser.AdminQueryKind.Player, "76561198000000044", 0 } );
var observer = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.Components.ReplacementAdminExample>()
	.Single( component => component.GameObject.Name == "Admin replacement verification" );
for ( var attempt = 0; attempt < 200 && observer.LastReply?.RequestId != id; attempt++ ) await GameTask.DelaySeconds( 0.05f );
if ( observer.LastReply?.RequestId != id || observer.LastReply.Status != SboxSuperuser.CommandStatus.Success
	|| observer.LastReply.History.Length < 2 ) throw new InvalidOperationException( "Authorized remote detail query failed." );
return new { Pass = true, HistoryRows = observer.LastReply.History.Length, RequestId = id };
