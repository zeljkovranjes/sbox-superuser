if ( Networking.IsHost ) throw new InvalidOperationException( "Connected remote test client required." );
var view = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Single();
var backend = Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.Components.IndependentChatExample>().Single();
var previousCount = view.PrivateResults;
var previousOverlay = ProjectSettings.Platform.ChatShowUI;
var observerObject = new GameObject( true, "T14 Failing Result Observer" );
var observer = observerObject.Components.Create<SuperuserCompatibilityLab.Components.FailingChatResultObserver>();
try
{
	view.Enabled = false;
	ProjectSettings.Platform.ChatShowUI = true;
	var requestId = Guid.NewGuid();
	backend.Submit( requestId, "/admin version" );
	for ( var attempt = 0; observer.Calls == 0 && attempt < 100; attempt++ ) await Task.Delay( 25, cancellationToken );
	if ( observer.Calls != 1 || view.PrivateResults != previousCount )
		throw new InvalidOperationException( "Disabled view received a result, or active observer was not invoked." );
	return new { Pass = true, requestId, observer.Calls, view.PrivateResults, ExpectedDefault = "Success: Superuser API 1" };
}
finally
{
	observerObject.Destroy();
	view.Enabled = true;
	ProjectSettings.Platform.ChatShowUI = previousOverlay;
}
