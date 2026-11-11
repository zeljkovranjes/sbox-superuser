using Sandbox;
using SboxSuperuser.Core.Commands;
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
internal sealed partial class ChatBoxRenderer : IDisposable
{
	private const int MaximumLines = 60;
	private const float FadeAfter = 8;
	private const float FadeLength = 1;
	private const int MaximumMessageLength = 256;

	private static readonly Vector4 Base = ChatTheme.Base;
	private static readonly Vector4 Raised = ChatTheme.Raised;
	private static readonly Vector4 Hover = ChatTheme.Hover;
	private static readonly Vector4 Pressed = ChatTheme.Pressed;
	private static readonly Vector4 Line = ChatTheme.Line;
	private static readonly Vector4 Shadow = new( 0, 0, 0, 0.85f );
	private static readonly Vector4 Text = ChatTheme.Text;
	private static readonly Vector4 Muted = ChatTheme.Muted;
	private static readonly Vector4 Notice = Rgb( 0xA8A8A8 );
	// Announcements lead with the tag in bold grey so they read as the server speaking, not a player.
	private static readonly Vector4 TagText = Rgb( 0xB4B4B4 );
	// A line that mentions you gets a soft gold wash and a solid gold edge, like Discord.
	private static readonly Vector4 MentionText = Rgb( 0xF5C451 );
	private static readonly Vector4 MentionWash = new( 0.96f, 0.77f, 0.32f, 0.16f );
	private static readonly Vector4 MentionEdge = Rgb( 0xF5C451 );
	private const float MentionEdgeWidth = 2;

	private readonly Scene _scene;
	private readonly ImGuiContext _context = new();
	private readonly CommandList _commands = new( "Superuser chat" ) { Flags = CommandList.Flag.Hud };
	private readonly List<ChatEntry> _lines = new();
	private readonly List<LineRun> _runs = new();
	private readonly SentHistory _history = new();
	private readonly Action<string> _openPlayer;
	private readonly Func<bool> _canOpenPlayer;
	private readonly Func<string, Completion> _complete;
	private static readonly Vector4 Scrollbar = new( 1, 1, 1, 0.18f );
	private const float ScrollbarWidth = 3;
	private AdminInputPanel _input;
	private MouseVisibility _previousMouse;
	private CameraComponent _camera;
	private string _draft = "";
	private bool _focusInput;
	// How far the history is scrolled up from the newest line, in pixels; only the wheel moves it, while open.
	private float _scrollBack;
	private const int WheelLines = 3;

	/// <summary>openPlayer shows a player in the admin menu; canOpenPlayer says whether that menu is there.</summary>
	/// <param name="complete">Autocomplete for a typed line; command hints sit between the history and the input.</param>
	internal ChatBoxRenderer( Scene scene, Action<string> openPlayer, Func<bool> canOpenPlayer, Func<string, Completion> complete )
	{
		_complete = complete;
		_scene = scene;
		_openPlayer = openPlayer;
		_canOpenPlayer = canOpenPlayer;
		ApplyTheme( _context.Style );
	}

	internal bool IsOpen => _input is not null;

	/// <param name="mentions">The message's @mentions; mentionsMe highlights the whole line for this player.</param>
	internal void AddPlayer( string steamId, string name, string message, IReadOnlyList<Mention> mentions, bool mentionsMe ) =>
		Add( new ChatEntry( steamId, name, message, RealTime.Now, Mentions: mentions, MentionsMe: mentionsMe ) );

	internal void AddSystem( string message ) => Add( new ChatEntry( null, null, message, RealTime.Now ) );

	internal void AddAnnouncement( string tag, string message ) => Add( new ChatEntry( null, null, message, RealTime.Now, tag ) );

	internal void Open()
	{
		if ( IsOpen ) return;
		_previousMouse = Mouse.Visibility;
		_input = new AdminInputPanel( _context.IO ) { Close = Close };
		_history.Reset();
		_focusInput = true;
		_scrollBack = 0;
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
		_scrollBack = 0;
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
			DrawHeader();
			// The input keeps its room open or closed, so sending a message (which closes the chat) doesn't move the
			// history; autocomplete floats above it instead of taking room.
			var inputHeight = ImGui.GetFrameHeightWithSpacing() + 4 * scale;
			// ImGui's own scroll is never used: it lags a frame behind, which made the history jump on open and close.
			ImGui.BeginChild( "lines", new Vector2( 0, -inputHeight ), ImGuiChildFlags.None,
				ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse );
			DrawLines( scale );
			ImGui.EndChild();
			if ( IsOpen ) DrawInput();
		}
		ImGui.End();
		ImGui.PopStyleVar();
	}

	// Laid out invisibly while closed, so the history has the same room open or closed.
	private void DrawHeader()
	{
		ImGui.PushStyleVar( ImGuiStyleVar.Alpha, IsOpen ? 1 : 0 );
		ImGui.TextColored( Text, "{0}", "Chat" );
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.PopStyleVar();
	}

	private void DrawLines( float scale )
	{
		var now = RealTime.Now;
		var system = _scene.GetSystem<PrefixSystem>();
		var clock = PrefixSystem.Now;
		var gap = 2 * scale;
		var names = IsOpen && _canOpenPlayer();
		// Newest lines sit right above the input. The lines shown this frame are summed first, so opening or closing
		// (which shows or hides faded lines) never lays out with the previous frame's height.
		MeasureNewLines( system, clock, names, gap );
		var total = 0f;
		foreach ( var line in _lines )
			if ( Alpha( line, now ) > 0 ) total += line.Height;
		if ( total <= 0 ) return;
		var room = ImGui.GetContentRegionAvail().y;
		if ( IsOpen && ImGui.IsWindowHovered() ) _scrollBack += ImGui.GetIO().MouseWheel * WheelLines * ImGui.GetTextLineHeightWithSpacing();
		_scrollBack = Math.Clamp( _scrollBack, 0, MathF.Max( 0, total - room ) );
		if ( IsOpen && total > room ) DrawScrollbar( room, total );
		ImGui.SetCursorPosY( room - total + _scrollBack );
		foreach ( var line in _lines )
		{
			var alpha = Alpha( line, now );
			if ( alpha <= 0 ) continue;
			var top = ImGui.GetCursorPosY();
			ImGui.PushStyleVar( ImGuiStyleVar.Alpha, alpha );
			DrawLine( line, system, clock, names );
			ImGui.PopStyleVar();
			ImGui.Dummy( new Vector2( 0, gap ) );
			line.Height = ImGui.GetCursorPosY() - top;
		}
	}

	/// <summary>
	/// Lays out lines that have never been drawn, invisibly and below the visible area, so a new message's real
	/// height (wrapped, with a mention) is known before the history is positioned and nothing jumps a frame later.
	/// </summary>
	private void MeasureNewLines( PrefixSystem system, double clock, bool names, float gap )
	{
		var hidden = ImGui.GetContentRegionAvail().y + 10000;
		foreach ( var line in _lines )
		{
			if ( line.Height > 0 ) continue;
			ImGui.SetCursorPosY( hidden );
			ImGui.PushStyleVar( ImGuiStyleVar.Alpha, 0 );
			DrawLine( line, system, clock, false );
			ImGui.PopStyleVar();
			ImGui.Dummy( new Vector2( 0, gap ) );
			line.Height = ImGui.GetCursorPosY() - hidden;
		}
		ImGui.SetCursorPosY( 0 );
	}

	/// <summary>A thin bar on the right edge while there is more history than fits; the wheel scrolls it.</summary>
	private void DrawScrollbar( float room, float total )
	{
		var origin = ImGui.GetCursorScreenPos();
		var right = origin.x + ImGui.GetContentRegionAvail().x;
		var thumb = MathF.Max( 12, room * room / total );
		// _scrollBack counts up from the newest line, so the thumb sits at the bottom until you scroll back.
		var travel = room - thumb;
		var top = origin.y + travel * (1 - _scrollBack / (total - room));
		ImGui.GetWindowDrawList().AddRectFilled( new Vector2( right - ScrollbarWidth, top ), new Vector2( right, top + thumb ),
			ImGui.GetColorU32( Scrollbar ), ScrollbarWidth );
	}

	private float Alpha( ChatEntry line, float now ) => IsOpen ? 1f : Math.Clamp( 1 - (now - line.Added - FadeAfter) / FadeLength, 0, 1 );

	// Closed, lines sit straight on the game with a thin dark shadow so they stay readable; open, the panel is behind them.
	private void DrawLine( ChatEntry line, PrefixSystem system, double clock, bool names )
	{
		var shadow = !IsOpen;
		var left = ImGui.GetCursorScreenPos().x;
		var right = left + ImGui.GetContentRegionAvail().x;
		if ( line.MentionsMe ) DrawMentionWash( line, left, right );
		ImGui.PushTextWrapPos( ImGui.GetContentRegionMax().x );
		if ( line.Tag is not null )
		{
			DrawTag( line.Tag, shadow );
			ImGui.SameLine();
			FlowText( line, Text, left, right, shadow );
		}
		else if ( line.SteamId is null )
		{
			FlowText( line, Notice, left, right, shadow );
		}
		else
		{
			DrawAvatar( line.SteamId );
			ImGui.SameLine();
			_runs.Clear();
			system.Render( line.SteamId, line.Name, clock, _runs );
			StyledText.Draw( _runs, shadow );
			if ( names ) NameLink( line.SteamId );
			ImGui.SameLine( 0, 0 );
			ShadowedText( ":", Muted, right, shadow );
			ImGui.SameLine();
			FlowText( line, Text, left, right, shadow );
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

	private void Add( ChatEntry line )
	{
		_lines.Add( line );
		if ( _lines.Count > MaximumLines ) _lines.RemoveAt( 0 );
		_scrollBack = 0;
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

	private static Vector4 Rgb( int hex ) => ChatTheme.Rgb( hex );

	public void Dispose()
	{
		Close();
		_commands.Reset();
		if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
		if ( ReferenceEquals( ImGuiContext.Current, _context ) ) ImGuiContext.Current = null;
	}

	private sealed record ChatEntry( string SteamId, string Name, string Message, float Added, string Tag = null,
		IReadOnlyList<Mention> Mentions = null, bool MentionsMe = false )
	{
		internal IReadOnlyList<Mention> Mentions { get; init; } = Mentions ?? Array.Empty<Mention>();

		internal WrappedText Wrap { get; set; }

		// Measured when last drawn; 0 until then.
		internal float Height { get; set; }
	}
}
