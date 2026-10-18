if ( Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Remote client required." );
var view = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Single();
var backend = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.Components.IndependentChatExample>().Single();
if ( view.PrivateResults != 0 ) throw new InvalidOperationException( "Use a fresh example view." );
async Task WaitForResults( int count )
{
	for ( var i = 0; view.PrivateResults < count && i < 200; i++ ) await Task.Delay( 25, cancellationToken );
	if ( view.PrivateResults != count ) throw new InvalidOperationException( "Unexpected private result count: " + view.PrivateResults );
}
view.SubmitText( "/admin example.increment platform-private" );
await WaitForResults( 1 );
var platformId = view.LastRequestId;
if ( !view.Lines.Any( line => line.Contains( "execution=1; actor=" + Connection.Local.SteamId + "; tag=platform-private" ) ) )
	throw new InvalidOperationException( "Platform path did not render one authenticated execution." );
await Task.Delay( 1000, cancellationToken );
view.SubmitText( "T14 platform ordinary" );
await Task.Delay( 1000, cancellationToken );
view.IndependentBackend = true;
view.SubmitText( "/admin example.increment independent-private" );
await WaitForResults( 2 );
var independentId = view.LastRequestId;
if ( !view.Lines.Any( line => line.Contains( "execution=2; actor=" + Connection.Local.SteamId + "; tag=independent-private" ) ) )
	throw new InvalidOperationException( "Independent path did not render one authenticated execution." );
await Task.Delay( 1000, cancellationToken );
backend.Submit( independentId, "/admin example.increment independent-private" );
await WaitForResults( 3 );
if ( view.Lines.Any( line => line.Contains( "execution=3" ) ) ) throw new InvalidOperationException( "Retry executed twice." );
await Task.Delay( 1000, cancellationToken );
view.SubmitText( "/admin group create t14forbidden" );
await WaitForResults( 4 );
if ( !view.Lines.Any( line => line.Contains( "Private • Denied" ) ) ) throw new InvalidOperationException( "Denial was not rendered privately." );
return new { Pass = true, platformId, independentId, DeniedId = view.LastRequestId, view.PrivateResults, view.OrdinaryMessages, view.Lines };
