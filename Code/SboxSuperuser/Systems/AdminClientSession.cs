using Sandbox;
using SboxSuperuser.Engine.Admin;

#pragma warning disable SB3002 // References intentionally invalidate pending requests when a connection changes.

namespace SboxSuperuser.Systems;

internal sealed class AdminClientSession : GameObjectSystem<AdminClientSession>
{
	private readonly Dictionary<Guid, DateTimeOffset> _pending = new();
	private readonly Dictionary<Guid, AdminReply> _received = new();
	private Connection _host;
	private Connection _local;

	public AdminClientSession( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -90, Maintain, nameof( AdminClientSession ) );
	}

	internal Guid Begin()
	{
		Maintain();
		if ( !Networking.IsActive || Application.IsDedicatedServer || Connection.Local is not { IsActive: true } )
			throw new InvalidOperationException( "An active player session is required." );
		if ( _pending.Count >= 8 ) throw new InvalidOperationException( "Wait for pending administration requests." );
		_host = Connection.Host;
		_local = Connection.Local;
		var id = Guid.NewGuid();
		_pending.Add( id, DateTimeOffset.UtcNow.AddSeconds( 15 ) );
		return id;
	}

	internal void Receive( AdminReply reply )
	{
		if ( reply is null || !CurrentConnection() || !_pending.ContainsKey( reply.RequestId ) ) return;
		// Deliver on the next scene update, after submission has returned its correlation ID.
		_received[ reply.RequestId ] = reply;
	}

	private bool CurrentConnection() => Scene.IsValid() && Scene == Game.ActiveScene && Networking.IsActive
		&& ReferenceEquals( _host, Connection.Host ) && ReferenceEquals( _local, Connection.Local ) && _local is { IsActive: true };

	private void Maintain()
	{
		if ( _pending.Count == 0 ) return;
		var connected = CurrentConnection();
		foreach ( var id in _pending.Where( pair => !connected || _received.ContainsKey( pair.Key ) || pair.Value <= DateTimeOffset.UtcNow )
			.Select( pair => pair.Key ).ToArray() )
		{
			_pending.Remove( id );
			_received.Remove( id, out var reply );
			Notify( connected && reply is not null ? reply : HostAdminQueries.Failure( id, CommandStatus.Unknown, connected
				? "Request timed out. Its outcome is unknown; inspect activity before retrying."
				: "Connection changed. The request outcome is unknown." ) );
		}
	}

	private void Notify( AdminReply reply )
	{
		Scene.RunEvent<IAdminResultEvent>( listener =>
		{
			try { listener.OnAdminResult( reply ); }
			catch ( Exception error ) { Log.Warning( $"[Superuser] Admin result listener failed; type={error.GetType().Name}." ); }
		} );
	}

	public override void Dispose()
	{
		_pending.Clear();
		_received.Clear();
		base.Dispose();
	}
}
