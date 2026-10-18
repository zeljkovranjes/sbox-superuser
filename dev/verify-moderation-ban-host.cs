if ( !Game.IsEditor || !Networking.IsHost || !SboxSuperuser.Superuser.IsOwner( Connection.Local ) ) throw new InvalidOperationException( "Isolated editor owner required." );
const string target = "90071996842377218";
if ( !Connection.All.Any( connection => connection.IsActive && connection.SteamId.ToString() == target ) ) throw new InvalidOperationException( "Real peer B must be connected." );
var requestId = Guid.NewGuid();
var result = await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
	new SboxSuperuser.CommandRequest( "ban", new[] { target, "1h", "T16 reconnect rejection" }, requestId ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
if ( result.Status != SboxSuperuser.CommandStatus.Success || Connection.All.Any( connection => connection.IsActive && connection.SteamId.ToString() == target ) )
	throw new InvalidOperationException( "Persisted ban did not confirm disconnect: " + result.Status );
var stored = await SboxSuperuser.Superuser.GetStore().ReadAsync( new SboxSuperuser.StoreKey( "moderation", "history" ), 1, cancellationToken );
if ( stored.Status != SboxSuperuser.StoreReadStatus.Found || !stored.Document.PayloadJson.Contains( result.Data[ "action" ] ) )
	throw new InvalidOperationException( "Disconnect lacked its persisted ban record." );
var page = await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( requestId: requestId ), cancellationToken );
if ( page.Entries.Single().Outcome != SboxSuperuser.CommandStatus.Success ) throw new InvalidOperationException( "Ban audit did not confirm outcome." );
return new { Pass = true, requestId, Action = result.Data[ "action" ], Expiry = result.Data[ "expiresUtc" ], DisconnectConfirmed = true, Revision = stored.Document.Revision };
