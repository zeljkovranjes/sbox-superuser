// Trusted development reflection invokes the client RPC wrapper, not a fabricated raw network packet.
if ( Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Remote isolated client required." );
var type = typeof( SboxSuperuser.Superuser ).Assembly.GetType( "SboxSuperuser.Engine.ChatIntegration.Platform.ChatReplies" );
var method = type.GetMethod( "Receive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic );
method.Invoke( null, new object[] { Guid.NewGuid(), SboxSuperuser.CommandStatus.Success, "t13-forged-reply-sentinel", Array.Empty<string>(), Array.Empty<string>() } );
return "Client wrapper invoked; verify neither client nor host displays the forged sentinel.";
