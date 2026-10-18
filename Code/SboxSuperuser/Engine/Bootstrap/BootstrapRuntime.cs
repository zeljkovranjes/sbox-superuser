using Sandbox;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed partial class BootstrapRuntime : IDisposable
{
	private readonly Scene _scene;
	private readonly Connection _host;
	private readonly bool _allowEditorTesting;
	private bool _disposed;
	private DateTimeOffset _nextAuditMaintenance = DateTimeOffset.UtcNow.AddSeconds( 60 );
	private Task _auditMaintenance;

	internal Guid Id { get; } = Guid.NewGuid();
	internal OwnerConfigurationFile Owners { get; }
	internal Storage.ConfiguredAdminStore Store { get; }
	internal Authority.HostPolicyService Policy { get; }
	internal Commands.CommandRegistry Commands { get; }
	internal Commands.HostCommandDispatcher Dispatcher { get; }
	internal Audit.HostAuditService Audit { get; }
	internal Core.Commands.ConsoleRootState ConsoleRoot { get; }
	internal Commands.AdministrationCommands Administration { get; }
	internal Modules.ModuleManager Modules { get; }
	internal ChatIntegration.Platform.PlatformChatAdapter Chat { get; }
	internal Moderation.HostModerationService Moderation { get; }
	internal Admin.HostAdminQueries AdminQueries { get; }
	internal Admin.AdminRequestGate AdminRequests { get; } = new();

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
			if ( !Moderation.IsUsable ) return BootstrapState.ModerationDegraded;

			if ( !ConsoleRoot.IsAvailable )
			{
				return BootstrapState.ConsoleUnavailable;
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

	internal BootstrapRuntime( Scene scene, bool allowEditorTesting, string commandRoot = "admin",
		Func<AdminStorageContext, IAdminStorageBackend> storageFactory = null )
	{
		_scene = scene;
		_host = Connection.Host;
		_allowEditorTesting = allowEditorTesting;
		Owners = new OwnerConfigurationFile( Application.IsDedicatedServer );
		var localStore = new Storage.HostAdminStore( Application.IsDedicatedServer, () => IsUsable );
		IAdminStorageBackend backend = localStore;
		try
		{
			if ( storageFactory is not null ) backend = storageFactory( new AdminStorageContext( Application.IsDedicatedServer, localStore ) );
			if ( backend is null ) throw new InvalidOperationException( "The storage factory returned null." );
		}
		catch ( Exception error )
		{
			backend = null;
			Log.Warning( $"[Superuser] Storage factory failed; type={error.GetType().Name}; no local fallback was selected." );
		}
		Store = new Storage.ConfiguredAdminStore( backend, () => ThreadSafe.IsMainThread && IsUsable,
			error => Log.Warning( $"[Superuser] Storage adapter failed; type={error.GetType().Name}." ) );
		Audit = new Audit.HostAuditService( Store, () => ThreadSafe.IsMainThread && IsUsable,
			error => Log.Warning( $"[Superuser] Audit storage degraded; type={error.GetType().Name}; files preserved." ) );
		Store.StorageChanged += Audit.OnStorageChanged;
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
		ConsoleRoot = new Core.Commands.ConsoleRootState( commandRoot, TypeLibrary.GetMemberAttributes<ConVarAttribute>().Select( attribute => attribute.Name ) );
		Modules = new Modules.ModuleManager( Commands, Policy, () => ThreadSafe.IsMainThread && IsUsable,
			error => Log.Warning( $"[Superuser] Module lifecycle failed; type={error.GetType().Name}; inspect module status." ),
			CommandDiscovery.AttributeCommandDiscovery.Discover );
		Moderation = new Moderation.HostModerationService( Store, ModerationAvailable, Dispatcher.CanUse, CanModerateTarget, ReportModerationFailure );
		Store.StorageChanged += Moderation.OnStorageChanged;
		AdminQueries = new Admin.HostAdminQueries( Policy, Moderation, Audit, OnlinePlayers );
		Administration = new Commands.AdministrationCommands( Policy, Audit, Commands, Dispatcher, ConsoleDiagnostics, Modules,
			Moderation, DisconnectModeratedAsync, NotifyModerationWarning );
		Chat = new ChatIntegration.Platform.PlatformChatAdapter( this );
		if ( !ConsoleRoot.IsAvailable )
		{
			Log.Warning( $"[Superuser] {ConsoleRoot.Message}" );
		}
	}

	private CommandResult ConsoleDiagnostics() => new( CommandStatus.Success, "Host administration health.", new Dictionary<string, string>
	{
		[ "mode" ] = Application.IsDedicatedServer ? "DedicatedServer" : "EditorTesting",
		[ "root" ] = ConsoleRoot.Name,
		[ "console" ] = ConsoleRoot.Message,
		[ "policy" ] = Policy.PolicyStatus.ToString(),
		[ "owners" ] = Policy.OwnerStatus.ToString(),
		[ "ownerSource" ] = Policy.OwnerSource,
		[ "audit" ] = Audit.Diagnostics().ReadStatus.ToString(),
		[ "auditDegraded" ] = Audit.IsDegraded.ToString(),
		[ "auditWrite" ] = Audit.Diagnostics().LastWriteStatus.ToString(),
		[ "retentionPending" ] = Audit.Diagnostics().RetentionPending.ToString(),
		[ "moderation" ] = Moderation.Status.ToString(),
		[ "migration" ] = "Player-hosted release games and host migration are unsupported."
	} );

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

	internal CommandResult CheckOrdinaryChat( Connection sender )
	{
		if ( !TryResolvePlayer( sender, out var actor ) )
		{
			return new CommandResult( CommandStatus.Denied, "An authenticated player connection is required." );
		}
		if ( !Policy.IsUsable )
		{
			return new CommandResult( CommandStatus.StorageUnavailable, "Authority state is unavailable." );
		}
		return Moderation.CheckRestriction( actor.SubjectId, Core.Moderation.ModerationKind.TextMute );
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
		if ( _disposed ) return;
		_disposed = true;
		Chat?.Dispose();
		Modules?.Dispose();
		Administration?.Dispose();
		Dispatcher?.Dispose();
		Store.StorageChanged -= Policy.OnStorageChanged;
		Store.StorageChanged -= Audit.OnStorageChanged;
		Store.StorageChanged -= Moderation.OnStorageChanged;
	}
}
