using Sandbox;
using SboxSuperuser.Core.Menu;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.UI;

namespace SboxSuperuser.Systems;

internal sealed class AdminMenuSystem : GameObjectSystem<AdminMenuSystem>
{
	private const float SaveInterval = 1;

	private readonly Dictionary<Guid, string> _registrations = new();
	private AdminMenuRenderer _view;
	private string _key;
	private MenuLayout _layout;
	private MenuPreferences _preferences;
	private int _savedLayoutRevision;
	private int _savedPreferencesRevision;
	private RealTimeSince _sinceSave;

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

	internal void Receive( AdminReply reply ) => _view?.Receive( reply );
	internal bool IsOpen => _view?.Interactive == true;

	internal void SetOpen( bool open )
	{
		if ( !open ) { Close(); return; }
		if ( !CanShow ) return;
		EnsureView();
		_view.SetInteractive( true );
	}

	private bool CanShow => _key is not null && Game.IsPlaying && Networking.IsActive && !Application.IsDedicatedServer;

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
		if ( !CanShow ) { Shutdown(); return; }
		try
		{
			if ( Input.Keyboard.Pressed( _key ) )
			{
				SetOpen( !IsOpen );
			}

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
			Log.Warning( $"[Superuser] Admin input disabled; check its Open Key. type={error.GetType().Name}." );
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
