using Sandbox;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed class BootstrapRuntime : IDisposable
{
	private readonly Scene _scene;
	private readonly Connection _host;
	private readonly bool _allowEditorTesting;
	private bool _disposed;
	private DateTimeOffset _nextAuditMaintenance = DateTimeOffset.UtcNow.AddSeconds( 60 );
	private Task _auditMaintenance;

	internal Guid Id { get; } = Guid.NewGuid();
	internal OwnerConfigurationFile Owners { get; }
	internal Storage.HostAdminStore Store { get; }
	internal Authority.HostPolicyService Policy { get; }
	internal Commands.CommandRegistry Commands { get; }
	internal Commands.HostCommandDispatcher Dispatcher { get; }
	internal Audit.HostAuditService Audit { get; }

	internal BootstrapState Health
	{
		get
		{
			if ( !Policy.IsUsable )
			{
				return Owners.State == BootstrapState.OwnersInvalid && Policy.OwnerSource == "bootstrap"
					? BootstrapState.OwnersInvalid : BootstrapState.AuthorityDegraded;
			}

			if ( Audit.IsDegraded )
			{
				return BootstrapState.AuditDegraded;
			}

			if ( Policy.OwnerSource == "bootstrap" )
			{
				return Owners.State;
			}

			return Policy.OwnerCount > 0 ? BootstrapState.Ready : BootstrapState.OwnersMissing;
		}
	}

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
		Store = new Storage.HostAdminStore( Application.IsDedicatedServer, () => IsUsable );
		Audit = new Audit.HostAuditService( Store, () => ThreadSafe.IsMainThread && IsUsable,
			error => Log.Warning( $"[Superuser] Audit storage degraded; type={error.GetType().Name}; files preserved." ) );
		var ownerStatus = Owners.State switch
		{
			BootstrapState.Ready => StoreReadStatus.Found,
			BootstrapState.OwnersMissing => StoreReadStatus.Missing,
			_ => StoreReadStatus.Corrupt
		};
		Policy = new Authority.HostPolicyService( Store, Owners.Configuration?.Ids ?? Array.Empty<string>(),
			ownerStatus, () => IsUsable,
			error => Log.Warning( $"[Superuser] Authority validation failed; type={error.GetType().Name}; files preserved." ), Audit );
		Store.StorageChanged += Policy.OnStorageChanged;
		Commands = new Commands.CommandRegistry( Policy.RegisteredPermissions, Policy.ReplaceRegisteredPermissions,
			() => ThreadSafe.IsMainThread && IsUsable );
		Dispatcher = new Commands.HostCommandDispatcher( Policy, Commands, () => ThreadSafe.IsMainThread && IsUsable,
			OnlinePlayers, error => Log.Warning( $"[Superuser] Command execution failed; type={error.GetType().Name}; outcome may be Unknown." ), audit: Audit );
	}

	internal void MaintainAudit()
	{
		if ( !IsUsable || DateTimeOffset.UtcNow < _nextAuditMaintenance || _auditMaintenance is { IsCompleted: false } )
		{
			return;
		}

		_nextAuditMaintenance = DateTimeOffset.UtcNow.AddSeconds( 60 );
		_auditMaintenance = Audit.MaintainAutomaticAsync( CancellationToken.None );
	}

	private IReadOnlyList<CommandPlayer> OnlinePlayers()
	{
		var players = new List<CommandPlayer>();
		foreach ( var connection in Connection.All )
		{
			if ( !TryResolvePlayer( connection, out var actor ) )
			{
				continue;
			}

			var name = connection.DisplayName;
			if ( string.IsNullOrWhiteSpace( name ) || name.Length > 256 || name.Any( char.IsControl ) )
			{
				name = actor.SubjectId;
			}

			players.Add( new CommandPlayer( actor.SubjectId, name ) );
			if ( players.Count > 1024 )
			{
				return null;
			}
		}

		return players.AsReadOnly();
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
			&& Policy.IsOwner( identity.SubjectId );
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

	/// <summary>Revokes this runtime so retained references cannot resolve identities.</summary>
	public void Dispose()
	{
		_disposed = true;
		Dispatcher.Dispose();
		Store.StorageChanged -= Policy.OnStorageChanged;
	}
}
