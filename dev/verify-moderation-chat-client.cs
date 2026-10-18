if ( Networking.IsHost ) throw new InvalidOperationException( "Remote client A required." );
var view = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Single();
if ( view.PrivateResults != 0 || view.OrdinaryMessages != 0 ) throw new InvalidOperationException( "Fresh test chat view required." );
view.SubmitText( "T16 muted platform" );
await Task.Delay( 1100, cancellationToken );
view.SubmitText( "/admin version" );
for ( var attempt = 0; view.PrivateResults < 2 && attempt < 100; attempt++ ) await Task.Delay( 25, cancellationToken );
if ( view.PrivateResults != 2 || view.OrdinaryMessages != 0 || !view.Lines.Any( line => line.Contains( "Text chat is muted" ) ) )
	throw new InvalidOperationException( "Platform mute or command escape failed." );
view.IndependentBackend = true;
await Task.Delay( 1100, cancellationToken );
view.SubmitText( "T16 muted independent" );
await Task.Delay( 1100, cancellationToken );
view.SubmitText( "/admin version" );
for ( var attempt = 0; view.PrivateResults < 3 && attempt < 100; attempt++ ) await Task.Delay( 25, cancellationToken );
if ( view.PrivateResults != 3 || view.OrdinaryMessages != 1 || view.Lines.Any( line => line.Contains( "T16 muted" ) ) )
	throw new InvalidOperationException( "Independent mute or command escape failed." );
return new { Pass = true, view.PrivateResults, PrivateSystemMessages = view.OrdinaryMessages, view.Lines };
