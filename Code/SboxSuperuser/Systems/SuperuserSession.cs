using Sandbox;
using SboxSuperuser.Core.Bootstrap;
using SboxSuperuser.Engine.Bootstrap;

namespace SboxSuperuser.Systems;

internal sealed partial class SuperuserSession : GameObjectSystem<SuperuserSession>
{
	private readonly BootstrapRegistrations _registrations = new();
	private BootstrapRuntime _runtime;
	private BootstrapState _state;
	private bool _disposed;
	private bool _stopping;
	private bool _restartRequired;
	private Func<AdminStorageContext, IAdminStorageBackend> _storageFactory;

	internal void ConfigureStorage( Func<AdminStorageContext, IAdminStorageBackend> factory )
	{
		if ( _runtime is not null || _registrations.Count != 0 || _disposed || _stopping || _restartRequired )
			throw new InvalidOperationException( "Configure storage before enabling any bootstrap in a fresh scene." );
		_storageFactory = factory;
	}

	/// <summary>Creates the scene-owned coordinator and one lifecycle callback.</summary>
	public SuperuserSession( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -100, Reconcile, nameof( SuperuserSession ) );
	}

	internal void Attach( Guid componentId, bool allowEditorTesting, string commandRoot = "admin" )
	{
		if ( _disposed || _stopping )
		{
			return;
		}
		_registrations.Attach( componentId, allowEditorTesting, commandRoot );
		Reconcile();
	}

	internal void Detach( Guid componentId )
	{
		_registrations.Detach( componentId );
		Reconcile();
	}

	internal BootstrapRuntime GetRuntime()
	{
		Reconcile();
		return _runtime;
	}

	internal BootstrapDiagnostics GetDiagnostics()
	{
		Reconcile();
		return new BootstrapDiagnostics( _state, _runtime?.Id, _registrations.Count,
			_runtime?.Policy.OwnerCount ?? 0, _runtime?.Owners.Path,
			_runtime is null ? _state.ToString() : $"{_runtime.Owners.Message} Policy={_runtime.Policy.PolicyStatus}; ownerSource={_runtime.Policy.OwnerSource}; ownerStatus={_runtime.Policy.OwnerStatus}; {_runtime.ConsoleRoot.Message}" );
	}

	private void Reconcile()
	{
		if ( _disposed || _stopping )
		{
			return;
		}
		if ( _restartRequired )
		{
			Stop( BootstrapState.RestartRequired );
			return;
		}
		if ( _registrations.Count == 0 )
		{
			Stop( BootstrapState.Disabled );
			return;
		}

		if ( !Scene.IsValid() || Scene != Game.ActiveScene || !Networking.IsHost
			|| !Networking.IsActive || Connection.Host is null || !Connection.Host.IsActive
			|| (!Application.IsDedicatedServer && (!Game.IsEditor || !_registrations.AllowsEditorTesting)) )
		{
			Stop( _registrations.Count > 1 ? BootstrapState.DuplicateBootstrap : BootstrapState.Unsupported );
			return;
		}

		if ( _registrations.Count > 1 )
		{
			Stop( BootstrapState.DuplicateBootstrap );
			return;
		}

		if ( _runtime is not null && !_runtime.IsUsable )
		{
			Stop( BootstrapState.Unsupported );
		}

		if ( _runtime is not null )
		{
			_runtime.MaintainAudit();
			SetState( _runtime.Health );
			return;
		}

		// Only on initialization: retire inactive scene runtimes before starting this one.
		// No static registry, per-frame scene scan or retained cross-scene subscription.
		foreach ( var otherScene in Sandbox.Scene.All )
		{
			if ( otherScene == Scene || !otherScene.IsValid() )
			{
				continue;
			}

			otherScene.GetSystem<SuperuserSession>()?.Stop( BootstrapState.Unsupported );
		}

		_runtime = new BootstrapRuntime( Scene, _registrations.AllowsEditorTesting, _registrations.CommandRoot, _storageFactory );
		SetState( _runtime.Health );
		Log.Info( $"[Superuser] Runtime {_runtime.Id} initialized; owners={_runtime.Owners.Configuration?.Count ?? 0}; file={_runtime.Owners.Path}" );
	}

	private void Stop( BootstrapState state )
	{
		if ( _runtime is not null )
		{
			var retiring = _runtime;
			_runtime = null;
			_stopping = true;
			try
			{
				Log.Info( $"[Superuser] Runtime {retiring.Id} stopped." );
				retiring.Dispose();
			}
			finally
			{
				_stopping = false;
			}
		}

		SetState( state );
	}

	private void SetState( BootstrapState state )
	{
		if ( _state == state )
		{
			return;
		}
		_state = state;
		if ( state == BootstrapState.DuplicateBootstrap )
		{
			Log.Warning( "[Superuser] Duplicate bootstrap detected; identity and owner access suspended. Keep one enabled bootstrap." );
		}
	}

	/// <summary>Retires the runtime and releases scene registrations.</summary>
	public override void Dispose()
	{
		_disposed = true;
		Stop( BootstrapState.Disposed );
		_registrations.Clear();
		base.Dispose();
	}
}
