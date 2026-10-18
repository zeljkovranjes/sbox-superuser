// Real client submission through the same platform path used by the in-game chatbox.
if ( Networking.IsHost || !Networking.IsActive || Connection.Local is null )
	throw new InvalidOperationException( "Run on one connected real test client." );
var messages = new[]
{
	"T13 ordinary chat passes",
	"!admin version",
	"/administrator version",
	"/admin version",
	"/admin group create t13forbidden",
	"/admin t13unknown",
	"/admin help \"unfinished",
	"/admin help version",
	"/admin version",
	"/admin version"
};
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var rate = Convert.ToDouble( typeof( Sandbox.Platform.Chat ).GetProperty( "RateLimit", flags ).GetValue( null ) );
var delay = Math.Max( 500, (int)(rate * 1000) + 250 );
foreach ( var message in messages )
{
	Sandbox.Platform.Chat.Say( message );
	await Task.Delay( delay, cancellationToken );
}
return new { Connection.Local.Id, Connection.Local.SteamId, Submitted = messages, DelayMilliseconds = delay };
