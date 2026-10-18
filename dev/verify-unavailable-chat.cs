if ( Networking.IsHost ) throw new InvalidOperationException( "Remote isolated sender required." );
var view = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Single();
if ( view.PrivateResults != 4 || view.OrdinaryMessages != 2 ) throw new InvalidOperationException( "Run the ordinary privacy sequence first." );
view.IndependentBackend = false;
view.SubmitText( "/admin group create t19-unavailable-private-sentinel" );
for ( var i = 0; view.PrivateResults < 5 && i < 100; i++ ) await Task.Delay( 25, cancellationToken );
if ( view.PrivateResults != 5 || !view.Lines.Any( line => line.Contains( "Private • Unsupported" ) ) ) throw new InvalidOperationException( "Unavailable platform command was not answered privately once." );
await Task.Delay( 1000, cancellationToken );
view.IndependentBackend = true;
view.SubmitText( "/admin group create t19-independent-private-sentinel" );
await Task.Delay( 1000, cancellationToken );
if ( view.PrivateResults != 5 || view.OrdinaryMessages != 2 || view.Lines.Any( line => line.Contains( "private-sentinel" ) || line.Contains( "forged-reply-sentinel" ) ) )
	throw new InvalidOperationException( "Unavailable transport leaked command text or duplicated its response." );
return new { Pass = true, view.PrivateResults, view.OrdinaryMessages, PlatformUnavailablePrivate = true, IndependentUnavailableDropped = true };
