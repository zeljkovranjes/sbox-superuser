using Sandbox;
using SboxSuperuser.UI;

namespace SboxSuperuser.Systems;

internal sealed class AdminMenuSystem : GameObjectSystem<AdminMenuSystem>
{
	private readonly Dictionary<Guid, string> _registrations = new();
	private AdminMenuRenderer _view;
	private string _key;

	public AdminMenuSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -80, UpdateInput, nameof( AdminMenuSystem ) );
		Listen( Stage.FinishUpdate, 100, Render, nameof( AdminMenuSystem ) );
	}

	internal void Attach( Guid id, string key )
	{
		_registrations[ id ] = key;
		Close();
		_key = _registrations.Count == 1 ? key : null;
	}

	internal void Detach( Guid id )
	{
		_registrations.Remove( id );
		Close();
		_key = _registrations.Count == 1 ? _registrations.Values.First() : null;
	}

	internal void Receive( AdminReply reply ) => _view?.Model.Receive( reply );
	internal bool IsOpen => _view is not null;

	internal void SetOpen( bool open )
	{
		if ( !open ) { Close(); return; }
		if ( _view is null && _key is not null && Game.IsPlaying && Networking.IsActive && !Application.IsDedicatedServer )
			_view = new AdminMenuRenderer( Scene );
	}

	private void UpdateInput()
	{
		if ( _key is null || Application.IsDedicatedServer || !Game.IsPlaying || !Networking.IsActive ) { Close(); return; }
		try
		{
			if ( Input.Keyboard.Pressed( _key ) )
			{
				SetOpen( !IsOpen );
			}
			_view?.CaptureInput();
			if ( _view is { Open: false } ) Close();
		}
		catch ( Exception error )
		{
			Close();
			_key = null;
			Log.Warning( $"[Superuser] Admin input disabled; check its Open Key. type={error.GetType().Name}." );
		}
	}

	private void Render()
	{
		try { _view?.Draw(); }
		catch ( Exception error )
		{
			Close();
			Log.Warning( $"[Superuser] Admin view closed after render failure; type={error.GetType().Name}." );
		}
		if ( _view is { Open: false } ) Close();
	}

	private void Close()
	{
		_view?.Dispose();
		_view = null;
	}

	public override void Dispose()
	{
		Close();
		_registrations.Clear();
		base.Dispose();
	}
}
