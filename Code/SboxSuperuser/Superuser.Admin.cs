using Sandbox;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Systems;

#pragma warning disable SB3006 // Dedicated administration deliberately does not survive host migration.

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Requests a private host-filtered view; receive its matching ID through IAdminResultEvent.</summary>
	public static Guid RequestAdminQuery( AdminQuery query )
	{
		ArgumentNullException.ThrowIfNull( query );
		var id = Client().Begin();
		if ( Networking.IsHost ) _ = RunAdminQueryAsync( Connection.Local, id, query.Kind, query.Search, query.Page );
		else ForwardAdminQuery( id, query.Kind, query.Search, query.Page );
		return id;
	}

	/// <summary>Submits one exact-target moderation action; confirmation belongs to your view, authorization to the host.</summary>
	public static Guid RequestAdminAction( AdminActionRequest request )
	{
		ArgumentNullException.ThrowIfNull( request );
		var id = Client().Begin();
		if ( Networking.IsHost ) _ = RunAdminActionAsync( Connection.Local, id, request.Action, request.SteamId, request.Reason, request.Duration );
		else ForwardAdminAction( id, request.Action, request.SteamId, request.Reason, request.Duration );
		return id;
	}

	private static AdminClientSession Client()
	{
		if ( !ThreadSafe.IsMainThread || !Game.ActiveScene.IsValid() )
			throw new InvalidOperationException( "Use the active scene's main thread." );
		return Game.ActiveScene.GetSystem<AdminClientSession>();
	}

	[Rpc.Host]
	private static void ForwardAdminQuery( Guid id, AdminQueryKind kind, string search, int page )
		=> _ = RunAdminQueryAsync( Rpc.Caller, id, kind, search, page );

	[Rpc.Host]
	private static void ForwardAdminAction( Guid id, AdminAction action, string steamId, string reason, string duration )
		=> _ = RunAdminActionAsync( Rpc.Caller, id, action, steamId, reason, duration );

	private static Task RunAdminQueryAsync( Connection caller, Guid id, AdminQueryKind kind, string search, int page )
		=> RunAdminRequestAsync( caller, id, runtime => runtime.AdminQueries.QueryAsync(
			() => runtime.TryResolvePlayer( caller, out var actor ) ? actor : null, new AdminQuery( kind, search, page ), id, CancellationToken.None ) );

	private static Task RunAdminActionAsync( Connection caller, Guid id, AdminAction action, string steamId, string reason, string duration )
		=> RunAdminRequestAsync( caller, id, async runtime =>
		{
			var request = new AdminActionRequest( action, steamId, reason, duration );
			var result = await runtime.Dispatcher.SubmitAsync( () => runtime.TryResolvePlayer( caller, out var actor ) ? actor : null,
				CommandOrigin.Interface, request.ToCommand( id ), CancellationToken.None );
			return HostAdminQueries.Failure( id, result.Status, result.Message );
		} );

	private static async Task RunAdminRequestAsync( Connection caller, Guid id,
		Func<Engine.Bootstrap.BootstrapRuntime, Task<AdminReply>> execute )
	{
		var runtime = FindAuthorityRuntime();
		if ( id == Guid.Empty || caller is null || runtime is null || !runtime.TryResolvePlayer( caller, out _ ) )
		{
			SendAdminReply( caller, HostAdminQueries.Failure( id, CommandStatus.Unsupported, "An actual dedicated host or opted-in editor test is required." ) );
			return;
		}
		if ( !runtime.AdminRequests.TryEnter( caller.Id, DateTimeOffset.UtcNow ) )
		{
			SendAdminReply( caller, HostAdminQueries.Failure( id, CommandStatus.Conflict, "Too many administration requests; wait before trying again." ) );
			return;
		}
		try
		{
			var reply = await execute( runtime );
			if ( runtime.TryResolvePlayer( caller, out _ ) ) SendAdminReply( caller, reply );
		}
		catch ( ArgumentException )
		{
			SendAdminReply( caller, HostAdminQueries.Failure( id, CommandStatus.InvalidArguments, "Invalid administration input. Use an exact Steam ID, printable reason and explicit duration." ) );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Administration request failed; type={error.GetType().Name}." );
			SendAdminReply( caller, HostAdminQueries.Failure( id, CommandStatus.Unknown, "No confirmed outcome; inspect activity before retrying." ) );
		}
		finally { runtime.AdminRequests.Exit( caller.Id ); }
	}

	private static void SendAdminReply( Connection caller, AdminReply reply )
	{
		if ( !Networking.IsHost || caller is not { IsActive: true } || !Connection.All.Any( current => ReferenceEquals( current, caller ) ) ) return;
		if ( ReferenceEquals( caller, Connection.Local ) && !Application.IsDedicatedServer )
		{
			Client().Receive( reply );
			return;
		}
		using ( Rpc.FilterInclude( caller ) ) ReceiveAdminReply( System.Text.Json.JsonSerializer.Serialize( reply ) );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private static void ReceiveAdminReply( string json )
	{
		if ( Application.IsDedicatedServer || Rpc.Caller is not { IsHost: true } || json is null || json.Length > 65536 ) return;
		try { Client().Receive( System.Text.Json.JsonSerializer.Deserialize<AdminReply>( json ) ); }
		catch ( Exception error ) { Log.Warning( $"[Superuser] Invalid private admin reply; type={error.GetType().Name}." ); }
	}
}
