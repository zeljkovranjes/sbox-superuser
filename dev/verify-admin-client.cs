// Run on each real local test client. No desktop input or engine mocks are used.
if ( Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "A connected test client is required." );
var go = new GameObject( Game.ActiveScene ) { Name = "Admin replacement verification" };
var observer = go.Components.Create<SuperuserCompatibilityLab.Components.ReplacementAdminExample>();
observer.Search( "" );
for ( var attempt = 0; attempt < 200 && observer.LastReply?.RequestId != observer.RequestId; attempt++ ) await GameTask.DelaySeconds( 0.05f );
if ( observer.LastReply?.Status != SboxSuperuser.CommandStatus.Denied ) throw new InvalidOperationException( "Non-staff client directory was not denied." );
observer.ConfirmedAction( new SboxSuperuser.AdminActionRequest( SboxSuperuser.AdminAction.Warn, "76561198000000044", "Denied remote warning" ) );
for ( var attempt = 0; attempt < 200 && observer.LastReply?.RequestId != observer.RequestId; attempt++ ) await GameTask.DelaySeconds( 0.05f );
if ( observer.LastReply?.Status != SboxSuperuser.CommandStatus.Denied ) throw new InvalidOperationException( "Non-staff client action was not denied." );
return new { Pass = true, Connection.Local.SteamId, observer.LastReply.Status };
