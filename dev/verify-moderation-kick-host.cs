if ( !Game.IsEditor || !Networking.IsHost ) throw new InvalidOperationException( "Isolated editor host required." );
const string target = "90071996842377218";
if ( !Connection.All.Any( connection => connection.IsActive && connection.SteamId.ToString() == target ) ) throw new InvalidOperationException( "Unbanned real peer B must have reconnected." );
var request = new SboxSuperuser.CommandRequest( "kick", new[] { target, "T16 kick without ban" }, Guid.NewGuid() );
var result = await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local, request, SboxSuperuser.CommandOrigin.Console, cancellationToken );
if ( result.Status != SboxSuperuser.CommandStatus.Success || Connection.All.Any( connection => connection.IsActive && connection.SteamId.ToString() == target ) )
	throw new InvalidOperationException( "Real kick did not confirm disconnect: " + result.Status );
var history = await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
	new SboxSuperuser.CommandRequest( "moderation.history", new[] { target }, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
if ( history.Status != SboxSuperuser.CommandStatus.Success || history.Data.Keys.Count( key => key.StartsWith( "row" ) ) != 3 ) throw new InvalidOperationException( "Ban, unban and kick history was not retained." );
return new { Pass = true, ReconnectAfterUnban = true, DisconnectConfirmed = true, History = history.Data };
