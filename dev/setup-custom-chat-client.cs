if ( Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Connected remote test client required." );
if ( Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.UI.CustomChatExample>().Any() )
	throw new InvalidOperationException( "Example view already exists; inspect before repeating setup." );
if ( Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.Components.IndependentChatExample>().Count() != 1 )
	throw new InvalidOperationException( "Expected one replicated independent backend." );
var previousOverlay = ProjectSettings.Platform.ChatShowUI;
ProjectSettings.Platform.ChatShowUI = false;
var local = new GameObject( true, "T14 Local Custom Chat View" );
local.Components.Create<ScreenPanel>();
var view = local.Components.Create<SuperuserCompatibilityLab.UI.CustomChatExample>();
await Task.Delay( 100, cancellationToken );
return new { local.Id, view.PrivateResults, PreviousOverlay = previousOverlay, Connection.Local.SteamId };
