using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string PlayersWindow = "players";
	private const string ActivityWindow = "activity";
	private const string AboutWindow = "about";
	private const string RestrictionsWindow = "restrictions";
	private const string CommandsWindow = "commands";
	private const string PermissionsWindow = "permissions";
	private const string ChatWindow = "chat";
	private const string DocumentationWindow = "documentation";
	private const string PrefixesWindow = "prefixes";

	private const ImGuiWindowFlags PassiveFlags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus;

	/// <summary>
	/// Begins a window whose open, pinned and placement state lives in the layout. Returns false when the
	/// window is hidden; otherwise the caller draws its content when expanded and then calls ImGui.End.
	/// </summary>
	private bool BeginWindow( string name, string title, Vector2 defaultOffset, Vector2 defaultSize, Vector2 minimumSize, out bool expanded )
	{
		expanded = false;
		if ( !_layout.IsVisible( name, Interactive ) ) return false;

		var window = _layout.Window( name );
		if ( window.HasBounds )
		{
			ImGui.SetNextWindowPos( new Vector2( window.X, window.Y ), ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSize( new Vector2( window.Width, window.Height ), ImGuiCond.FirstUseEver );
		}
		else
		{
			ImGui.SetNextWindowPos( new Vector2( 0, ImGui.GetFrameHeight() ) + defaultOffset * Scale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSize( defaultSize * Scale, ImGuiCond.FirstUseEver );
		}

		ImGui.SetNextWindowSizeConstraints( minimumSize * Scale, new Vector2( float.MaxValue, float.MaxValue ) );
		var open = true;
		expanded = Interactive
			? ImGui.Begin( title, ref open, ImGuiWindowFlags.NoSavedSettings )
			: ImGui.Begin( title, ImGuiWindowFlags.NoSavedSettings | PassiveFlags );
		DrawPinButton( name, window.Pinned );

		// Passive windows cannot move, and a collapsed window reports only its title bar height.
		if ( Interactive && !ImGui.IsWindowCollapsed() )
		{
			var position = ImGui.GetWindowPos();
			var size = ImGui.GetWindowSize();
			_layout.SetBounds( name, position.x, position.y, size.x, size.y );
		}
		if ( !open ) _layout.SetOpen( name, false );
		return true;
	}

	/// <summary>Pin toggle drawn in the title bar, left of the close button.</summary>
	private void DrawPinButton( string name, bool pinned )
	{
		var style = ImGui.GetStyle();
		var size = ImGui.GetFontSize();
		var position = ImGui.GetWindowPos();
		var windowSize = ImGui.GetWindowSize();
		var closeWidth = Interactive ? size + style.ItemInnerSpacing.x : 0;
		var right = position.x + windowSize.x - style.WindowBorderSize - style.FramePadding.x - closeWidth;
		var min = new Vector2( right - size, position.y + style.FramePadding.y );

		var cursor = ImGui.GetCursorScreenPos();
		ImGui.PushClipRect( position, position + windowSize, false );
		ImGui.SetCursorScreenPos( min );
		if ( ImGui.InvisibleButton( "##pin", new Vector2( size, size ) ) ) _layout.TogglePin( name );
		var hovered = ImGui.IsItemHovered();
		ImGui.SetItemTooltip( "{0}", pinned ? "Unpin" : "Pin: keep on screen while the menu is closed" );
		DrawPinGlyph( min, size, pinned, hovered );
		ImGui.PopClipRect();
		ImGui.SetCursorScreenPos( cursor );
	}

	private static void DrawPinGlyph( Vector2 min, float size, bool pinned, bool hovered )
	{
		var draw = ImGui.GetWindowDrawList();
		var center = min + new Vector2( size, size ) * 0.5f;
		if ( hovered ) draw.AddCircleFilled( center, size * 0.5f, ImGui.GetColorU32( ImGuiCol.ButtonHovered ) );

		var color = ImGui.GetColorU32( pinned ? ImGuiCol.CheckMark : hovered ? ImGuiCol.Text : ImGuiCol.TextDisabled );
		// Upright when pinned, tilted when loose, like the usual pin icon.
		var direction = pinned ? new Vector2( 0, 1 ) : new Vector2( -0.7071f, 0.7071f );
		var across = new Vector2( -direction.y, direction.x );
		var shoulder = center - direction * size * 0.12f;
		var thickness = MathF.Max( 1, size * 0.09f );
		draw.AddLine( shoulder, center + direction * size * 0.4f, color, thickness );
		draw.AddLine( shoulder - across * size * 0.22f, shoulder + across * size * 0.22f, color, thickness );
		var head = shoulder - direction * size * 0.16f;
		if ( pinned ) draw.AddCircleFilled( head, size * 0.15f, color );
		else draw.AddCircle( head, size * 0.15f, color, 0, thickness );
	}
}
