// Trusted development reflection invokes the client-side RPC wrapper; the engine must refuse HostOnly sending.
if ( Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Remote isolated client required." );
var type = typeof( SboxSuperuser.Superuser ).Assembly.GetType( "SboxSuperuser.Engine.Console.ConsoleReplies" );
var method = type.GetMethod( "Receive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic );
method.Invoke( null, new object[] { Guid.NewGuid(), SboxSuperuser.CommandStatus.Success, "t11-forged-reply-sentinel", Array.Empty<string>(), Array.Empty<string>() } );
return "Client wrapper invoked; verify neither client nor host logs a Superuser reply containing the sentinel.";
