// Continue after the T14 client script and attempted client-side forged reply wrapper.
if ( Networking.IsHost ) throw new InvalidOperationException( "Remote isolated sender required." );
var view = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Single();
if ( view.PrivateResults != 4 || view.Lines.Any( line => line.Contains( "forged-reply-sentinel" ) ) ) throw new InvalidOperationException( "Forged private reply was accepted." );
view.IndependentBackend = true;
view.SubmitText( "T19 independent ordinary" );
for ( var i = 0; view.OrdinaryMessages < 2 && i < 100; i++ ) await Task.Delay( 25, cancellationToken );
if ( view.OrdinaryMessages != 2 ) throw new InvalidOperationException( "Independent ordinary message was not delivered once." );
return new { Pass = true, view.PrivateResults, view.OrdinaryMessages, ForgedReplyRejected = true };
