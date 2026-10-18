// Run on client A of a two-client isolated editor session before configuring owners.
if ( Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Connected remote lab client required." );
var help = Guid.NewGuid();
var denied = Guid.NewGuid();
ConsoleSystem.Run( "admin", "help" );
ConsoleSystem.Run( "superuser_request", help.ToString( "N" ), "admin", "help", "version" );
ConsoleSystem.Run( "superuser_request", help.ToString( "N" ), "admin", "help", "version" );
ConsoleSystem.Run( "superuser_request", denied.ToString( "N" ), "admin", "user", Connection.Local.SteamId.ToString(), "group", "add", "owner" );
return new { Submitted = true, HelpRequest = help, DeniedRequest = denied, Caller = Connection.Local.SteamId.ToString(), Connection = Connection.Local.Id };
