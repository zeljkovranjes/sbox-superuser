using Sandbox;
using Sandbox.Rendering;
using Sandbox.UI;
using SboxSuperuser.Core.Menu;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

/// <summary>
/// Draws the admin menu. Interactive while the menu is open; passive (no input, pinned windows only)
/// while it is closed with windows pinned.
/// </summary>
internal sealed partial class AdminMenuRenderer : IDisposable
{
	private readonly Scene _scene;
	private readonly ImGuiContext _context = new();
	private readonly CommandList _commands = new( "Superuser admin menu" ) { Flags = CommandList.Flag.Hud };
	private readonly MenuLayout _layout;
	private readonly MenuPreferences _preferences;
	private readonly string _openKey;
	private AdminInputPanel _input;
	private MouseVisibility _previousMouse;
	private CameraComponent _camera;
	internal AdminMenuModel Model { get; } = new( Superuser.RequestAdminQuery, Superuser.RequestAdminAction );
	internal AdminServerModel Server { get; } = new( Superuser.RequestAdminQuery, Superuser.RequestAdminCommand );
	internal bool Interactive { get; private set; }

	/// <summary>Set when the user asks to close the menu (Esc, the close item or the input panel).</summary>
	internal bool CloseRequested { get; private set; }

	internal AdminMenuRenderer( Scene scene, MenuLayout layout, MenuPreferences preferences, string openKey )
	{
		_scene = scene;
		_layout = layout;
		_preferences = preferences;
		_openKey = openKey;
		if ( preferences.LastDuration.Length > 0 ) Model.Duration = preferences.LastDuration;
		_layout.FitTo( Screen.Width, Screen.Height );
	}

	/// <summary>Layout used when nothing is saved yet: only the player list is open.</summary>
	internal static MenuLayout DefaultLayout()
	{
		var layout = new MenuLayout();
		layout.SetOpen( PlayersWindow, true );
		return layout;
	}

	internal void SetInteractive( bool interactive )
	{
		CloseRequested = false;
		if ( Interactive == interactive ) return;
		if ( interactive )
		{
			_previousMouse = Mouse.Visibility;
			_input = new AdminInputPanel( _context.IO )
			{
				Close = HandleEscape,
				OpenKey = _openKey,
				Toggle = () => CloseRequested = true
			};
			Interactive = true;
			return;
		}

		Model.Cancel();
		_pendingRun = null;
		ReleaseInput();
		Interactive = false;
	}

	internal void CaptureInput()
	{
		if ( !Interactive ) return;
		Mouse.Visibility = MouseVisibility.Visible;
		Input.ClearActions();
		Input.AnalogMove = 0;
		Input.AnalogLook = default;
		if ( Input.EscapePressed )
		{
			Input.EscapePressed = false;
			HandleEscape();
		}
	}

	// Esc backs out of a pending confirmation before it closes the whole menu.
	private void HandleEscape()
	{
		if ( Model.Confirmation is not null ) Model.Cancel();
		else if ( _pendingRun is not null ) _pendingRun = null;
		else CloseRequested = true;
	}

	/// <summary>Routes a host reply to the model that asked for it.</summary>
	internal void Receive( AdminReply reply )
	{
		if ( !Server.Receive( reply ) )
		{
			Model.Receive( reply );
			return;
		}
		// A finished command may have changed the selected player or the lists, so show its result and reload.
		if ( ReferenceEquals( Server.LastResult, reply ) )
		{
			_showCommandResult = true;
			Notify( $"{Server.LastCommand}: {reply.Status}. {reply.Message}", AdminMenuTheme.Tone( reply.Status ) );
			Model.Refresh();
		}
	}

	internal void Draw()
	{
		var now = DateTimeOffset.UtcNow;
		Model.Tick( now );
		Server.RequestCommands();
		if ( _layout.IsVisible( RestrictionsWindow, Interactive ) ) Server.RequestRestrictions( now );
		if ( _layout.IsVisible( PermissionsWindow, Interactive ) ) Server.RequestPermissions();
		if ( _layout.IsVisible( ChatWindow, Interactive ) || _wantPlayerChat ) Server.RequestChat( now );
		_wantPlayerChat = false;
		var previous = ImGuiContext.Current;
		ImGuiContext.Current = _context;
		try
		{
			_context.IO.DeltaTime = MathF.Max( RealTime.Delta, 0.0001f );
			// Passive windows must not react to a cursor the game owns.
			_context.IO.MousePos = Interactive ? Mouse.Position : new Vector2( -float.MaxValue, -float.MaxValue );
			ImGui.NewFrame();
			if ( Interactive ) DrawMenuBar();
			DrawPlayersWindow();
			DrawRestrictionsWindow();
			DrawChatWindow();
			DrawCommandsWindow();
			DrawActivityWindow();
			DrawPermissionsWindow();
			DrawAboutWindow();
			DrawNotices();
			if ( Interactive )
			{
				DrawConfirmation();
				DrawRunConfirmation();
			}
			var lists = ImGui.Render();
			if ( _camera != _scene.Camera )
			{
				if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
				_camera = _scene.Camera;
				if ( _camera.IsValid() ) _camera.AddCommandList( _commands, Sandbox.Rendering.Stage.AfterUI, 10000 );
			}
			_commands.Reset();
			using var painter = Painter.Begin( _commands );
			foreach ( var list in lists ) list.Render( painter );
		}
		finally { ImGuiContext.Current = previous; }
	}

	private void ReleaseInput()
	{
		if ( _input is not null )
		{
			InputFocus.Clear( _input );
			_input.Delete( true );
			_input = null;
		}
		for ( var button = 0; button < 3; button++ ) _context.IO.AddMouseButtonEvent( button, false );
		// Only an interactive menu changed the cursor; a passive one leaves it to the game.
		if ( Interactive && Mouse.Visibility == MouseVisibility.Visible ) Mouse.Visibility = _previousMouse;
	}

	public void Dispose()
	{
		// Always release: a panel left behind keeps keyboard focus and swallows every key, including the open key.
		ReleaseInput();
		_commands.Reset();
		if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
		ImGuiHost.ClipboardCopyProvider = null;
		if ( ReferenceEquals( ImGuiContext.Current, _context ) ) ImGuiContext.Current = null;
	}
}
