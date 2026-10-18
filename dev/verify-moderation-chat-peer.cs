var view = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Single();
if ( view.PrivateResults != 0 || view.OrdinaryMessages != 0 || view.Lines.Count != 0 )
	throw new InvalidOperationException( "Peer received suppressed text or private feedback." );
return new { Pass = true, view.PrivateResults, view.OrdinaryMessages };
