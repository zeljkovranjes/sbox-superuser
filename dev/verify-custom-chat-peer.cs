if ( Networking.IsHost ) throw new InvalidOperationException( "Connected remote test client required." );
var view = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Single();
if ( view.PrivateResults != 0 || view.OrdinaryMessages != 2 || view.Lines.Any( line => line.Contains( "private" ) ) )
	throw new InvalidOperationException( "Peer privacy or ordinary delivery check failed." );
return new { Pass = true, view.PrivateResults, view.OrdinaryMessages, view.Lines };
