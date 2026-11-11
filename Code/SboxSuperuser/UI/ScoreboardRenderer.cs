using Sandbox;
using Sandbox.Rendering;
using SboxSuperuser.Core.Prefixes;
using SboxSuperuser.Engine.Vendor.ImGui;
using SboxSuperuser.Systems;

namespace SboxSuperuser.UI;

/// <summary>
/// The scoreboard: every connected player with their avatar, prefix, name and ping, in the chat box's colours.
/// Right-click a player for their Steam profile, and staff also get moderation actions that open the admin menu.
/// </summary>
internal sealed class ScoreboardRenderer : IDisposable
{
	private static readonly AdminAction[] StaffActions = { AdminAction.Kick, AdminAction.Warn, AdminAction.Mute, AdminAction.Gag, AdminAction.Ban };
	private const float Width = 640;
	private const float RowHeight = 28;
	private const string SteamProfileUrl = "https://steamcommunity.com/profiles/";

	private readonly ImGuiContext _context = new();
	private readonly CommandList _commands = new( "Superuser scoreboard" ) { Flags = CommandList.Flag.Hud };
	private readonly Scene _scene;
	private readonly List<LineRun> _runs = new();
	private readonly Dictionary<string, Texture> _avatars = new();
	private CameraComponent _camera;

	internal ScoreboardRenderer( Scene scene )
	{
		_scene = scene;
		ChatTheme.Apply( _context.Style );
	}

	/// <param name="interactive">Whether the cursor is free, so rows can be right-clicked.</param>
	internal void Draw( bool interactive )
	{
		var previous = ImGuiContext.Current;
		ImGuiContext.Current = _context;
		try
		{
			_context.IO.DeltaTime = MathF.Max( RealTime.Delta, 0.0001f );
			_context.IO.MousePos = interactive ? Mouse.Position : new Vector2( -float.MaxValue, -float.MaxValue );
			ImGui.NewFrame();
			DrawBoard();
			var lists = ImGui.Render();
			if ( _camera != _scene.Camera )
			{
				if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
				_camera = _scene.Camera;
				if ( _camera.IsValid() ) _camera.AddCommandList( _commands, Sandbox.Rendering.Stage.AfterUI, 8900 );
			}
			_commands.Reset();
			using var painter = Painter.Begin( _commands );
			foreach ( var list in lists ) list.Render( painter );
		}
		finally { ImGuiContext.Current = previous; }
	}

	/// <summary>Stops drawing, for frames the scoreboard is hidden.</summary>
	internal void Clear() => _commands.Reset();

	internal void AddMouseButton( int button, bool down ) => _context.IO.AddMouseButtonEvent( button, down );

	private void DrawBoard()
	{
		var scale = _context.AppliedStyleScale;
		var players = Connection.All.Where( connection => connection.IsActive ).OrderBy( connection => connection.DisplayName, StringComparer.OrdinalIgnoreCase ).ToArray();
		var height = (96 + players.Length * RowHeight) * scale;
		var size = new Vector2( Width * scale, MathF.Min( height, Screen.Height * 0.8f ) );
		ImGui.SetNextWindowPos( new Vector2( Screen.Width, Screen.Height ) * 0.5f, ImGuiCond.Always, new Vector2( 0.5f, 0.5f ) );
		ImGui.SetNextWindowSize( size );
		var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings
			| ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar;
		if ( ImGui.Begin( "##superuser-scoreboard", flags ) )
		{
			ImGui.TextColored( ChatTheme.Text, "{0}", Networking.ServerName is { Length: > 0 } server ? server : "Players" );
			ImGui.SameLine( ImGui.GetContentRegionMax().x - ImGui.CalcTextSize( $"{players.Length} / {Networking.MaxPlayers}" ).x );
			ImGui.TextDisabled( "{0} / {1}", players.Length, Networking.MaxPlayers );
			ImGui.Separator();
			DrawTable( players );
		}
		ImGui.End();
	}

	private void DrawTable( Connection[] players )
	{
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;
		if ( !ImGui.BeginTable( "players", 2, flags ) ) return;
		ImGui.TableSetupColumn( "Player", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableSetupColumn( "Ping", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Ping 000" ).x );
		ImGui.TableHeadersRow();
		var prefixes = _scene.GetSystem<PrefixSystem>();
		var staff = _scene.GetSystem<AdminMenuSystem>()?.IsAvailable == true;
		for ( var row = 0; row < players.Length; row++ )
		{
			var player = players[ row ];
			var steamId = player.SteamId.ToString();
			ImGui.TableNextRow( ImGuiTableRowFlags.None, RowHeight * _context.AppliedStyleScale );
			ImGui.TableNextColumn();
			ImGui.PushID( row );
			ImGui.Selectable( "##row", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap );
			DrawMenu( steamId, staff );
			ImGui.SameLine();
			DrawAvatar( steamId );
			ImGui.SameLine();
			_runs.Clear();
			prefixes?.Render( steamId, player.DisplayName, PrefixSystem.Now, _runs );
			if ( _runs.Count > 0 ) StyledText.Draw( _runs );
			else ImGui.TextUnformatted( player.DisplayName );
			ImGui.TableNextColumn();
			ImGui.TextDisabled( "{0}", player.IsHost ? "host" : player.Ping.ToString() );
			ImGui.PopID();
		}
		ImGui.EndTable();
	}

	private static void DrawMenu( string steamId, bool staff )
	{
		if ( !ImGui.BeginPopupContextItem( "player-menu" ) ) return;
		if ( ImGui.MenuItem( "Open Steam profile..." ) ) Game.Overlay.OpenUrl( SteamProfileUrl + steamId );
		if ( ImGui.MenuItem( "Copy Steam ID" ) ) Sandbox.UI.Clipboard.SetText( steamId );
		if ( staff )
		{
			ImGui.Separator();
			var menu = Game.ActiveScene?.GetSystem<AdminMenuSystem>();
			if ( ImGui.MenuItem( "Open in admin menu" ) ) menu?.ShowPlayer( steamId );
			foreach ( var action in StaffActions )
			{
				ImGui.PushStyleColor( ImGuiCol.Text, AdminMenuTheme.Tone( action ) );
				var chosen = ImGui.MenuItem( action + "..." );
				ImGui.PopStyleColor();
				if ( chosen ) menu?.ShowPlayerAction( steamId, action );
			}
		}
		ImGui.EndPopup();
	}

	private void DrawAvatar( string steamId )
	{
		if ( !_avatars.TryGetValue( steamId, out var avatar ) ) _avatars[ steamId ] = avatar = Texture.Load( $"avatarsmall:{steamId}", false );
		var size = ImGui.GetTextLineHeight() + 4;
		var position = ImGui.GetCursorScreenPos() - new Vector2( 0, 2 );
		if ( avatar is not null )
			ImGui.GetWindowDrawList().AddImageRounded( avatar, position, position + new Vector2( size, size ), Vector2.Zero, Vector2.One, ImGui.GetColorU32( Vector4.One ), 4 );
		ImGui.Dummy( new Vector2( size, ImGui.GetTextLineHeight() ) );
	}

	public void Dispose()
	{
		_commands.Reset();
		if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
		if ( ReferenceEquals( ImGuiContext.Current, _context ) ) ImGuiContext.Current = null;
	}
}
