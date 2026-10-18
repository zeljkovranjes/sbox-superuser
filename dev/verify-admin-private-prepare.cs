// Trusted test instrumentation: a peer deliberately knows the other client's request ID.
// If the host broadcasts a private reply, this peer will be able to correlate and expose it.
if ( Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "A connected test client is required." );
var observer = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.Components.ReplacementAdminExample>()
	.Single( component => component.GameObject.Name == "Admin replacement verification" );
var id = Guid.Parse( "082a5560-f11d-4de5-9bd8-8849eeefc261" );
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
var facade = typeof( SboxSuperuser.Superuser );
var session = facade.GetMethod( "Client", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic ).Invoke( null, null );
var pending = (System.Collections.Generic.Dictionary<Guid, DateTimeOffset>)session.GetType().GetField( "_pending", flags ).GetValue( session );
pending[ id ] = DateTimeOffset.UtcNow.AddMinutes( 5 );
observer.GetType().GetProperty( "RequestId" ).SetValue( observer, id );
return new { Ready = true, Connection.Local.SteamId, RequestId = id };
