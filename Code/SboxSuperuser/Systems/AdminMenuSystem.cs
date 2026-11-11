using Sandbox;
using SboxSuperuser.Core.Menu;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.UI;

namespace SboxSuperuser.Systems;

internal sealed class AdminMenuSystem : GameObjectSystem<AdminMenuSystem>
{
	private const float SaveInterval = 1;
	private const double AccessRefreshSeconds = 5;
	private const double AccessLifetimeSeconds = 10;

	private readonly Dictionary<Guid, string> _registrations = new();
	private AdminMenuRenderer _view;
	private string _key;
	private MenuLayout _layout;
	private MenuPreferences _preferences;
	private int _savedLayoutRevision;
	private int _savedPreferencesRevision;
	private RealTimeSince _sinceSave;
	private Connection _accessHost;
	private Guid _accessRequest;
	private double _nextAccessCheck;
	private double _accessUntil;
	private bool _authorized;
	private bool _openRequested;
	private string _selectedPlayer;
	private AdminAction? _selectedAction;

	public AdminMenuSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -80, UpdateInput, nameof( AdminMenuSystem ) );
		Listen( Stage.FinishUpdate, 100, Render, nameof( AdminMenuSystem ) );
	}

	internal void Attach( Guid id, string key )
	{
		_registrations[ id ] = key;
		Shutdown();
		_key = _registrations.Count == 1 ? key : null;
	}

	internal void Detach( Guid id )
	{
		_registrations.Remove( id );
		Shutdown();
		_key = _registrations.Count == 1 ? _registrations.Values.First() : null;
	}

	internal void Receive( AdminReply reply )
	{
		if ( reply.RequestId != _accessRequest ) { _view?.Receive( reply ); return; }
		_accessRequest = Guid.Empty;
		_authorized = reply.Status == CommandStatus.Success && ReferenceEquals( _accessHost, Connection.Host );
		_accessUntil = RealTime.Now + AccessLifetimeSeconds;
		if ( !_authorized ) { _openRequested = false; Shutdown(); return; }
		if ( !_openRequested || !CanShow ) return;
		_openRequested = false;
		EnsureView();
		_view.SetInteractive( true );
		if ( _selectedPlayer is not null ) _view.ShowPlayer( _selectedPlayer, _selectedAction );
		_selectedPlayer = null;
		_selectedAction = null;
	}
	internal bool IsOpen => _view?.Interactive == true;

	internal void SetOpen( bool open )
	{
		if ( !open ) { Close(); return; }
		if ( !SessionReady ) return;
		_openRequested = true;
		RequestAccess();
	}

	/// <summary>Whether the menu can open now: its component is in the scene and a game session is running.</summary>
	internal bool IsAvailable => CanShow;

	/// <summary>Opens the menu on the Players window with this player selected; the host still decides what they may see.</summary>
	internal void ShowPlayer( string steamId )
	{
		_selectedPlayer = steamId;
		SetOpen( true );
	}

	/// <summary>Opens the menu on this player with the action's popup ready for a reason; the host checks it as usual.</summary>
	internal void ShowPlayerAction( string steamId, AdminAction action )
	{
		_selectedAction = action;
		ShowPlayer( steamId );
	}

	// A joining client updates its systems before its own connection is active; requests then would throw.
	private bool SessionReady => _key is not null && Game.IsPlaying && Networking.IsActive && !Application.IsDedicatedServer && Connection.Local is { IsActive: true };
	private bool CanShow => SessionReady && _authorized && ReferenceEquals( _accessHost, Connection.Host ) && RealTime.Now < _accessUntil;

	private void RequestAccess()
	{
		if ( _accessRequest != Guid.Empty ) return;
		_accessHost = Connection.Host;
		_nextAccessCheck = RealTime.Now + AccessRefreshSeconds;
		// The host already restricts the player directory to staff, including module-defined moderators.
		_accessRequest = Superuser.RequestAdminQuery( new AdminQuery( AdminQueryKind.Players ) );
	}

	private void EnsureView()
	{
		LoadSettings();
		_view ??= new AdminMenuRenderer( Scene, _layout, _preferences, _key );
	}

	private void LoadSettings()
	{
		if ( _layout is not null ) return;
		_layout = MenuSettingsStore.LoadLayout() ?? AdminMenuRenderer.DefaultLayout();
		_preferences = MenuSettingsStore.LoadPreferences() ?? new MenuPreferences();
		_savedLayoutRevision = _layout.Revision;
		_savedPreferencesRevision = _preferences.Revision;
	}

	private void UpdateInput()
	{
		if ( !SessionReady || !ReferenceEquals( _accessHost, Connection.Host ) )
		{
			_authorized = false;
			_accessRequest = Guid.Empty;
			_accessHost = Connection.Host;
			_nextAccessCheck = 0;
			Shutdown();
			if ( !SessionReady ) { _openRequested = false; return; }
		}
		try
		{
			if ( RealTime.Now >= _nextAccessCheck ) RequestAccess();
			if ( Input.Keyboard.Pressed( _key ) )
			{
				SetOpen( !IsOpen );
			}

			if ( !CanShow ) { Shutdown(); return; }

			// Pinned windows from the last session come back as soon as the menu may show.
			LoadSettings();
			if ( _view is null && _layout.HasPinned ) EnsureView();
			_view?.CaptureInput();
			if ( _view is { CloseRequested: true } ) Close();
		}
		catch ( Exception error )
		{
			Shutdown();
			_key = null;
			Log.Warning( $"[Superuser] Admin input disabled; check its Open Key. type={error.GetType().Name}: {error.Message}" );
		}
	}

	private void Render()
	{
		try { _view?.Draw(); }
		catch ( Exception error )
		{
			Shutdown();
			Log.Warning( $"[Superuser] Admin view closed after render failure; type={error.GetType().Name}." );
		}
		if ( _view is { CloseRequested: true } ) Close();
		SaveSettings( false );
	}

	/// <summary>Closes the menu; pinned windows stay on screen without taking input.</summary>
	private void Close()
	{
		_openRequested = false;
		_selectedPlayer = null;
		if ( _view is null ) return;
		if ( _layout.HasPinned ) _view.SetInteractive( false );
		else Shutdown();
	}

	private void Shutdown()
	{
		_view?.Dispose();
		_view = null;
		SaveSettings( true );
	}

	private void SaveSettings( bool now )
	{
		if ( _layout is null ) return;
		var layoutChanged = _layout.Revision != _savedLayoutRevision;
		var preferencesChanged = _preferences.Revision != _savedPreferencesRevision;
		if ( !layoutChanged && !preferencesChanged ) return;
		if ( !now && _sinceSave < SaveInterval ) return;
		if ( layoutChanged ) MenuSettingsStore.SaveLayout( _layout );
		if ( preferencesChanged ) MenuSettingsStore.SavePreferences( _preferences );
		_savedLayoutRevision = _layout.Revision;
		_savedPreferencesRevision = _preferences.Revision;
		_sinceSave = 0;
	}

	public override void Dispose()
	{
		Shutdown();
		_registrations.Clear();
		base.Dispose();
	}
}
