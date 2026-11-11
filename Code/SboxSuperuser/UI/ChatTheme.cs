using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

/// <summary>The dark, flat look of Superuser's chat box, shared by every panel that should match it.</summary>
internal static class ChatTheme
{
	internal static readonly Vector4 Base = Rgb( 0x1A1A1A );
	internal static readonly Vector4 Raised = Rgb( 0x242424 );
	internal static readonly Vector4 Hover = Rgb( 0x2B2B2B );
	internal static readonly Vector4 Pressed = Rgb( 0x313131 );
	internal static readonly Vector4 Line = Rgb( 0x2E2E2E );
	internal static readonly Vector4 Text = Rgb( 0xE8E8E8 );
	internal static readonly Vector4 Muted = Rgb( 0x8C8C8C );

	internal static void Apply( ImGuiStyle style )
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
		style[ ImGuiCol.PopupBg ] = Raised;
		style[ ImGuiCol.ChildBg ] = Vector4.Zero;
		style[ ImGuiCol.Border ] = Line;
		style[ ImGuiCol.Separator ] = Line;
		style[ ImGuiCol.Text ] = Text;
		style[ ImGuiCol.TextDisabled ] = Muted;
		style[ ImGuiCol.FrameBg ] = Raised;
		style[ ImGuiCol.FrameBgHovered ] = Hover;
		style[ ImGuiCol.FrameBgActive ] = Pressed;
		style[ ImGuiCol.Header ] = Raised;
		style[ ImGuiCol.HeaderHovered ] = Hover;
		style[ ImGuiCol.HeaderActive ] = Pressed;
		style[ ImGuiCol.TableRowBg ] = Vector4.Zero;
		style[ ImGuiCol.TableRowBgAlt ] = new Vector4( 1, 1, 1, 0.025f );
		style[ ImGuiCol.TableHeaderBg ] = Base;
		style[ ImGuiCol.TableBorderLight ] = Line;
		style[ ImGuiCol.TableBorderStrong ] = Line;
		style[ ImGuiCol.ScrollbarBg ] = Vector4.Zero;
		style[ ImGuiCol.ScrollbarGrab ] = Rgb( 0x3D3D3D );
		style[ ImGuiCol.NavCursor ] = Vector4.Zero;
	}

	internal static Vector4 Rgb( int hex ) => new( ((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, 1 );
}
