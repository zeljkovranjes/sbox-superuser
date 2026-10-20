using Sandbox;
using Sandbox.Rendering;
using SboxSuperuser.Core.Chat;
using SboxSuperuser.Core.Prefixes;
using SboxSuperuser.Engine.Vendor.ImGui;
using SboxSuperuser.Systems;

namespace SboxSuperuser.UI;

/// <summary>
/// The chat box shown instead of the built-in one while prefixes are on. It behaves like the built-in box (bottom
/// left, lines fade after a while, the chat key opens the input) but draws names with their prefixes, in a neutral
/// dark theme: a #1A1A1A panel with #242424 fields.
/// </summary>
internal sealed class ChatBoxRenderer : IDisposable
{
	private const int MaximumLines = 60;
	private const float FadeAfter = 8;
	private const float FadeLength = 1;
	private const int MaximumMessageLength = 256;

	private static readonly Vector4 Base = Rgb( 0x1A1A1A );
	private static readonly Vector4 Raised = Rgb( 0x242424 );
	private static readonly Vector4 Hover = Rgb( 0x2B2B2B );
	private static readonly Vector4 Pressed = Rgb( 0x313131 );
	private static readonly Vector4 Line = Rgb( 0x2E2E2E );
	private static readonly Vector4 Shadow = new( 0, 0, 0, 0.85f );
	private static readonly Vector4 Text = Rgb( 0xE8E8E8 );
	private static readonly Vector4 Muted = Rgb( 0x8C8C8C );
	private static readonly Vector4 Notice = Rgb( 0xA8A8A8 );

	private readonly Scene _scene;
	private readonly ImGuiContext _context = new();
	private readonly CommandList _commands = new( "Superuser chat" ) { Flags = CommandList.Flag.Hud };
	private readonly List<ChatEntry> _lines = new();
	private readonly List<LineRun> _runs = new();
	private readonly SentHistory _history = new();
	private readonly Action<string> _openPlayer;
	private readonly Func<bool> _canOpenPlayer;
	private AdminInputPanel _input;
	private MouseVisibility _previousMouse;
	private CameraComponent _camera;
	private string _draft = "";
	private bool _focusInput;
	private bool _scrollToBottom;

	/// <summary>openPlayer shows a player in the admin menu; canOpenPlayer says whether that menu is there.</summary>
	internal ChatBoxRenderer( Scene scene, Action<string> openPlayer, Func<bool> canOpenPlayer )
	{
		_scene = scene;
		_openPlayer = openPlayer;
		_canOpenPlayer = canOpenPlayer;
		ApplyTheme( _context.Style );
	}

	internal bool IsOpen => _input is not null;

	internal void AddPlayer( string steamId, string name, string message ) => Add( new ChatEntry( steamId, name, message, RealTime.Now ) );

	internal void AddSystem( string message ) => Add( new ChatEntry( null, null, message, RealTime.Now ) );

	internal void Open()
	{
		if ( IsOpen ) return;
		_previousMouse = Mouse.Visibility;
		_input = new AdminInputPanel( _context.IO ) { Close = Close };
		_history.Reset();
		_focusInput = true;
		_scrollToBottom = true;
	}

	internal void Close()
	{
		if ( _input is null ) return;
		Sandbox.UI.InputFocus.Clear( _input );
		_input.Delete( true );
		_input = null;
		for ( var button = 0; button < 3; button++ ) _context.IO.AddMouseButtonEvent( button, false );
		if ( Mouse.Visibility == MouseVisibility.Visible ) Mouse.Visibility = _previousMouse;
		_draft = "";
	}

	/// <summary>While typing, the game gets no movement, look or actions.</summary>
	internal void CaptureInput()
	{
		if ( !IsOpen ) return;
		Mouse.Visibility = MouseVisibility.Visible;
		Input.ClearActions();
		Input.AnalogMove = 0;
		Input.AnalogLook = default;
		if ( !Input.EscapePressed ) return;
		Input.EscapePressed = false;
		Close();
	}

	internal void Draw()
	{
		var previous = ImGuiContext.Current;
		ImGuiContext.Current = _context;
		try
		{
			_context.IO.DeltaTime = MathF.Max( RealTime.Delta, 0.0001f );
			_context.IO.MousePos = IsOpen ? Mouse.Position : new Vector2( -float.MaxValue, -float.MaxValue );
			ImGui.NewFrame();
			DrawBox();
			var lists = ImGui.Render();
			if ( _camera != _scene.Camera )
			{
				if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
				_camera = _scene.Camera;
				if ( _camera.IsValid() ) _camera.AddCommandList( _commands, Sandbox.Rendering.Stage.AfterUI, 9000 );
			}
			_commands.Reset();
			using var painter = Painter.Begin( _commands );
			foreach ( var list in lists ) list.Render( painter );
		}
		finally { ImGuiContext.Current = previous; }
	}

	private void DrawBox()
	{
		var scale = _context.AppliedStyleScale;
		var size = new Vector2( 680, 360 ) * scale;
		ImGui.SetNextWindowPos( new Vector2( 24 * scale, Screen.Height - size.y - 180 * scale ) );
		ImGui.SetNextWindowSize( size );
		var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings
			| ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar;
		if ( !IsOpen ) flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground;
		ImGui.PushStyleVar( ImGuiStyleVar.WindowBorderSize, IsOpen ? 1 : 0 );
		if ( ImGui.Begin( "##superuser-chat", flags ) )
		{
			if ( IsOpen ) DrawHeader();
			var inputHeight = IsOpen ? ImGui.GetFrameHeightWithSpacing() + 4 * scale : 0;
			ImGui.BeginChild( "lines", new Vector2( 0, -inputHeight ), ImGuiChildFlags.None,
				IsOpen ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse );
			DrawLines( scale );
			if ( _scrollToBottom || !IsOpen ) ImGui.SetScrollHereY( 1 );
			_scrollToBottom = false;
			ImGui.EndChild();
			if ( IsOpen ) DrawInput();
		}
		ImGui.End();
		ImGui.PopStyleVar();
	}

	private static void DrawHeader()
	{
		ImGui.TextColored( Text, "{0}", "Chat" );
		ImGui.Separator();
		ImGui.Spacing();
	}

	private void DrawLines( float scale )
	{
		var now = RealTime.Now;
		var system = _scene.GetSystem<PrefixSystem>();
		var clock = PrefixSystem.Now;
		var gap = 2 * scale;
		var names = IsOpen && _canOpenPlayer();
		foreach ( var line in _lines )
		{
			var alpha = IsOpen ? 1f : Math.Clamp( 1 - (now - line.Added - FadeAfter) / FadeLength, 0, 1 );
			if ( alpha <= 0 ) continue;
			ImGui.PushStyleVar( ImGuiStyleVar.Alpha, alpha );
			DrawLine( line, system, clock, names );
			ImGui.PopStyleVar();
			ImGui.Dummy( new Vector2( 0, gap ) );
		}
	}

	// Closed, lines sit straight on the game with a thin dark shadow so they stay readable; open, the panel is behind them.
	private void DrawLine( ChatEntry line, PrefixSystem system, double clock, bool names )
	{
		var shadow = !IsOpen;
		var right = ImGui.GetCursorScreenPos().x + ImGui.GetContentRegionAvail().x;
		ImGui.PushTextWrapPos( ImGui.GetContentRegionMax().x );
		if ( line.SteamId is null )
		{
			ShadowedText( line.Message, Notice, right, shadow );
		}
		else
		{
			_runs.Clear();
			system.Render( line.SteamId, line.Name, clock, _runs );
			StyledText.Draw( _runs, shadow );
			if ( names ) NameLink( line.SteamId );
			ImGui.SameLine( 0, 0 );
			ShadowedText( ":", Muted, right, shadow );
			ImGui.SameLine();
			ShadowedText( line.Message, Text, right, shadow );
		}
		ImGui.PopTextWrapPos();
	}

	// A name is a link while the chat is open: hover underlines it, a click opens that player in the admin menu.
	private void NameLink( string steamId )
	{
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		if ( !ImGui.IsMouseHoveringRect( min, max ) ) return;
		ImGui.GetWindowDrawList().AddLine( new Vector2( min.x, max.y ), max, ImGui.GetColorU32( Muted ) );
		ImGui.SetMouseCursor( ImGuiMouseCursor.Hand );
		ImGui.SetTooltip( "{0}", "Open in the admin menu" );
		if ( ImGui.IsMouseClicked( ImGuiMouseButton.Left ) ) _openPlayer( steamId );
	}

	// The shadow is the same text, wrapped the same way, one pixel down and right.
	private static void ShadowedText( string text, Vector4 color, float right, bool shadow )
	{
		if ( shadow )
		{
			var position = ImGui.GetCursorScreenPos();
			ImGui.GetWindowDrawList().AddText( ImGui.GetFontSize(), position + Vector2.One, ImGui.GetColorU32( Shadow ), text,
				MathF.Max( 1, right - position.x ) );
		}
		ImGui.TextColored( color, "{0}", text );
	}

	private void DrawInput()
	{
		ImGui.Spacing();
		ImGui.SetNextItemWidth( -1 );
		if ( _focusInput )
		{
			ImGui.SetKeyboardFocusHere();
			_focusInput = false;
		}
		var submitted = ImGui.InputTextWithHint( "##say", "Type a message...", ref _draft,
			ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackHistory, BrowseHistory );
		if ( _draft.Length > MaximumMessageLength ) _draft = _draft.Substring( 0, MaximumMessageLength );
		if ( !submitted ) return;
		var text = _draft.Trim();
		if ( text.Length > 0 )
		{
			Sandbox.Platform.Chat.Say( text );
			_history.Add( text );
		}
		Close();
	}

	// Up and Down swap the input for an earlier message, and back to what was being typed.
	private int BrowseHistory( ImGuiInputTextCallbackData data )
	{
		var text = data.EventKey == ImGuiKey.UpArrow ? _history.Older( data.Buf ) : _history.Newer();
		if ( text is null ) return 0;
		data.DeleteChars( 0, data.BufTextLen );
		data.InsertChars( 0, text );
		return 0;
	}

	private void Add( ChatEntry line )
	{
		_lines.Add( line );
		if ( _lines.Count > MaximumLines ) _lines.RemoveAt( 0 );
		_scrollToBottom = true;
	}

	// Neutral greys only: the stock ImGui theme's blue never shows in the chat box.
	private static void ApplyTheme( ImGuiStyle style )
	{
		style.WindowRounding = 4;
		style.ChildRounding = 3;
		style.FrameRounding = 3;
		style.ScrollbarRounding = 3;
		style.WindowPadding = new Vector2( 12, 10 );
		style.FramePadding = new Vector2( 10, 6 );
		style.ItemSpacing = new Vector2( 6, 3 );
		style.ScrollbarSize = 8;
		style.WindowBorderSize = 1;

		style[ ImGuiCol.WindowBg ] = Base;
		style[ ImGuiCol.ChildBg ] = Vector4.Zero;
		style[ ImGuiCol.Border ] = Line;
		style[ ImGuiCol.Separator ] = Line;
		style[ ImGuiCol.Text ] = Text;
		style[ ImGuiCol.TextDisabled ] = Muted;
		style[ ImGuiCol.FrameBg ] = Raised;
		style[ ImGuiCol.FrameBgHovered ] = Hover;
		style[ ImGuiCol.FrameBgActive ] = Pressed;
		style[ ImGuiCol.TextSelectedBg ] = Rgb( 0x4A4A4A );
		style[ ImGuiCol.InputTextCursor ] = Text;
		style[ ImGuiCol.ScrollbarBg ] = Vector4.Zero;
		style[ ImGuiCol.ScrollbarGrab ] = Rgb( 0x3D3D3D );
		style[ ImGuiCol.ScrollbarGrabHovered ] = Rgb( 0x4A4A4A );
		style[ ImGuiCol.ScrollbarGrabActive ] = Rgb( 0x575757 );
		style[ ImGuiCol.NavCursor ] = Vector4.Zero;
	}

	private static Vector4 Rgb( int hex ) => new( ((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, 1 );

	public void Dispose()
	{
		Close();
		_commands.Reset();
		if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
		if ( ReferenceEquals( ImGuiContext.Current, _context ) ) ImGuiContext.Current = null;
	}

	private sealed record ChatEntry( string SteamId, string Name, string Message, float Added );
}
