using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

/// <summary>Navy palette and spacing shared by every admin menu view.</summary>
internal static class AdminMenuTheme
{
	internal static readonly Vector4 Background = Rgb( 0x0B1220 );
	internal static readonly Vector4 Surface = Rgb( 0x111A2E );
	internal static readonly Vector4 Raised = Rgb( 0x18233B );
	internal static readonly Vector4 Hover = Rgb( 0x22304D );
	internal static readonly Vector4 Border = Rgb( 0x26344F );
	internal static readonly Vector4 Text = Rgb( 0xE6EBF5 );
	internal static readonly Vector4 Muted = Rgb( 0x8C99B3 );
	internal static readonly Vector4 Accent = Rgb( 0x3B82F6 );
	internal static readonly Vector4 AccentHover = Rgb( 0x60A5FA );
	internal static readonly Vector4 AccentActive = Rgb( 0x2563EB );
	internal static readonly Vector4 Success = Rgb( 0x22C55E );
	internal static readonly Vector4 Warning = Rgb( 0xF59E0B );
	internal static readonly Vector4 Danger = Rgb( 0xEF4444 );
	internal static readonly Vector4 Neutral = Rgb( 0x64748B );

	internal const float TitleSize = 21;
	internal const float HeadingSize = 17;
	internal const float SmallSize = 13;

	internal static void Apply( ImGuiStyle style )
	{
		style.WindowRounding = 10;
		style.ChildRounding = 8;
		style.FrameRounding = 6;
		style.PopupRounding = 6;
		style.TabRounding = 6;
		style.ScrollbarRounding = 6;
		style.GrabRounding = 6;
		style.WindowBorderSize = 1;
		style.ChildBorderSize = 1;
		style.FrameBorderSize = 1;
		style.TabBarBorderSize = 1;
		style.WindowPadding = new Vector2( 20, 18 );
		style.FramePadding = new Vector2( 10, 6 );
		style.ItemSpacing = new Vector2( 10, 8 );
		style.ItemInnerSpacing = new Vector2( 8, 6 );
		style.CellPadding = new Vector2( 10, 6 );
		style.ScrollbarSize = 10;
		style.SeparatorTextBorderSize = 1;
		style.SeparatorTextPadding = new Vector2( 0, 4 );

		style[ ImGuiCol.Text ] = Text;
		style[ ImGuiCol.TextDisabled ] = Muted;
		style[ ImGuiCol.WindowBg ] = Fade( Background, 0.98f );
		style[ ImGuiCol.ChildBg ] = Surface;
		style[ ImGuiCol.PopupBg ] = Raised;
		style[ ImGuiCol.Border ] = Border;
		style[ ImGuiCol.BorderShadow ] = default;
		style[ ImGuiCol.FrameBg ] = Background;
		style[ ImGuiCol.FrameBgHovered ] = Raised;
		style[ ImGuiCol.FrameBgActive ] = Raised;
		style[ ImGuiCol.TitleBg ] = Surface;
		style[ ImGuiCol.TitleBgActive ] = Surface;
		style[ ImGuiCol.ScrollbarBg ] = default;
		style[ ImGuiCol.ScrollbarGrab ] = Border;
		style[ ImGuiCol.ScrollbarGrabHovered ] = Hover;
		style[ ImGuiCol.ScrollbarGrabActive ] = Neutral;
		style[ ImGuiCol.Button ] = Raised;
		style[ ImGuiCol.ButtonHovered ] = Hover;
		style[ ImGuiCol.ButtonActive ] = Border;
		style[ ImGuiCol.Header ] = Fade( Accent, 0.22f );
		style[ ImGuiCol.HeaderHovered ] = Hover;
		style[ ImGuiCol.HeaderActive ] = Fade( Accent, 0.32f );
		style[ ImGuiCol.Separator ] = Border;
		style[ ImGuiCol.Tab ] = default;
		style[ ImGuiCol.TabHovered ] = Hover;
		style[ ImGuiCol.TabSelected ] = Surface;
		style[ ImGuiCol.TabSelectedOverline ] = Accent;
		style[ ImGuiCol.TabDimmed ] = default;
		style[ ImGuiCol.TabDimmedSelected ] = Surface;
		style[ ImGuiCol.TableHeaderBg ] = Raised;
		style[ ImGuiCol.TableBorderStrong ] = Border;
		style[ ImGuiCol.TableBorderLight ] = Fade( Border, 0.6f );
		style[ ImGuiCol.TableRowBg ] = default;
		style[ ImGuiCol.TableRowBgAlt ] = Fade( Raised, 0.45f );
		style[ ImGuiCol.TextSelectedBg ] = Fade( Accent, 0.35f );
		style[ ImGuiCol.InputTextCursor ] = AccentHover;
		style[ ImGuiCol.NavCursor ] = AccentHover;
	}

	/// <summary>Maps a host outcome to its color; Unknown is a warning because it is never success.</summary>
	internal static Vector4 Tone( CommandStatus status ) => status switch
	{
		CommandStatus.Success => Success,
		CommandStatus.NoChange => Neutral,
		CommandStatus.Unknown or CommandStatus.Cancelled or CommandStatus.Conflict => Warning,
		_ => Danger
	};

	internal static Vector4 Tone( AdminAction action ) => action switch
	{
		AdminAction.Kick or AdminAction.Ban => Danger,
		AdminAction.Warn or AdminAction.Mute => Warning,
		_ => Success
	};

	internal static Vector4 Fade( Vector4 color, float alpha ) => new( color.x, color.y, color.z, alpha );

	private static Vector4 Rgb( int hex ) => new( ((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, 1 );
}
