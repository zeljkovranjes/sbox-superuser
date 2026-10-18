using Sandbox;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed class BootstrapRuntime : IDisposable
{
	private readonly Scene _scene;
	private readonly Connection _host;
	private readonly bool _allowEditorTesting;
	private bool _disposed;

	internal Guid Id { get; } = Guid.NewGuid();
	internal OwnerConfigurationFile Owners { get; }

	internal bool IsUsable => !_disposed && _scene.IsValid() && _scene == Game.ActiveScene
		&& Networking.IsActive && Networking.IsHost && _host is not null && _host.IsActive
		&& ReferenceEquals( _host, Connection.Host )
		&& (Application.IsDedicatedServer || (Game.IsEditor && _allowEditorTesting));

	internal BootstrapRuntime( Scene scene, bool allowEditorTesting )
	{
		_scene = scene;
		_host = Connection.Host;
		_allowEditorTesting = allowEditorTesting;
		Owners = new OwnerConfigurationFile( Application.IsDedicatedServer );
	}

	internal bool TryResolvePlayer( Connection connection, out ActorIdentity identity )
	{
		identity = null;
		if ( !IsUsable || connection is null || !connection.IsActive
			|| !Connection.All.Any( current => ReferenceEquals( current, connection ) ) )
		{
			return false;
		}

		try
		{
			identity = new ActorIdentity( ActorKind.Player, connection.SteamId.ToString() );
			return true;
		}
		catch ( ArgumentException )
		{
			// SteamId 0 (including the dedicated host connection) is not a player or console credential.
			return false;
		}
	}

	internal bool IsOwner( Connection connection )
	{
		return TryResolvePlayer( connection, out var identity )
			&& Owners.State == BootstrapState.Ready
			&& Owners.Configuration.Contains( identity.SubjectId );
	}

	// Called only by the separate non-forwarded dedicated-console entry, never the player route.
	internal bool TryResolveDedicatedConsole( out ActorIdentity identity )
	{
		identity = null;
		if ( !IsUsable || !Application.IsDedicatedServer )
		{
			return false;
		}

		identity = new ActorIdentity( ActorKind.DedicatedConsole, "dedicated-console" );
		return true;
	}

	public void Dispose()
	{
		_disposed = true;
	}
}
