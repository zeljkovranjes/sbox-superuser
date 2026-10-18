if ( !Networking.IsHost || !Game.IsEditor || !SboxSuperuser.Superuser.IsOwner( Connection.Local ) ) throw new InvalidOperationException( "Configured isolated editor owner required." );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
Task<SboxSuperuser.CommandResult> Run( string command, params string[] arguments ) => SboxSuperuser.Superuser.ExecuteCommandAsync(
	Connection.Local, new SboxSuperuser.CommandRequest( command, arguments, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
const string offline = "76561198000000044";
Require( (await Run( "warn", offline, "Offline warning" )).Status == SboxSuperuser.CommandStatus.Success, "Offline warning failed." );
Require( (await Run( "kick", offline, "Offline kick" )).Status == SboxSuperuser.CommandStatus.InvalidArguments, "Offline kick accepted." );
Require( (await Run( "ban", offline, "2h", "Offline ban" )).Status == SboxSuperuser.CommandStatus.Success, "Offline ban failed." );
Require( (await Run( "ban", offline, "1h", "Must not shorten" )).Status == SboxSuperuser.CommandStatus.Conflict, "Active ban replaced silently." );
Require( (await Run( "unban", offline )).Status == SboxSuperuser.CommandStatus.Success, "Offline unban failed." );
Require( (await Run( "unban", offline )).Status == SboxSuperuser.CommandStatus.NoChange, "Repeated unban changed history." );
Require( (await Run( "mute", offline, "1s", "Short mute" )).Status == SboxSuperuser.CommandStatus.Success, "Offline mute failed." );
await Task.Delay( 1150, cancellationToken );
Require( (await Run( "unmute", offline )).Status == SboxSuperuser.CommandStatus.NoChange, "Expired mute was still active." );
Require( (await Run( "ban", Connection.Local.SteamId.ToString(), "permanent", "Self" )).Status == SboxSuperuser.CommandStatus.Denied, "Owner self-punishment accepted." );
var history = await Run( "moderation.history", offline );
Require( history.Status == SboxSuperuser.CommandStatus.Success && history.Data.Keys.Count( key => key.StartsWith( "row" ) ) == 4, "History lost or repeated records." );
var player = Connection.All.Single( connection => connection.SteamId == 90071996842377217UL );
var peer = Connection.All.Single( connection => connection.SteamId == 90071996842377218UL );
Require( (await Run( "warn", player.SteamId.ToString(), "T16 private warning to A" )).Status == SboxSuperuser.CommandStatus.Success, "Online warning failed." );
Require( (await Run( "mute", player.SteamId.ToString(), "5m", "T16 chat mute" )).Status == SboxSuperuser.CommandStatus.Success, "Online mute failed." );
Require( SboxSuperuser.Superuser.CheckOrdinaryChat( player ).Status == SboxSuperuser.CommandStatus.Denied
	&& SboxSuperuser.Superuser.CheckOrdinaryChat( peer ).Status == SboxSuperuser.CommandStatus.Success, "Mute targeted the wrong player." );
return new { Pass = true, OfflineRows = 4, MutedPlayer = player.SteamId, WarningRecipient = player.Id, Peer = peer.Id };
