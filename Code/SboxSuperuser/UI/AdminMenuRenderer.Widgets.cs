using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

// Every host- or player-supplied string is drawn as text or a "{0}" argument, never as an ImGui ID or format string.
internal sealed partial class AdminMenuRenderer
{
	private float Scale => _context.AppliedStyleScale;

	private static void Heading( string text, float size = AdminMenuTheme.HeadingSize )
	{
		ImGui.PushFont( null, size );
		ImGui.TextUnformatted( text );
		ImGui.PopFont();
	}

	/// <summary>Small uppercase label above a group of controls.</summary>
	private static void Label( string text )
	{
		ImGui.PushFont( null, AdminMenuTheme.SmallSize );
		ImGui.TextColored( AdminMenuTheme.Muted, "{0}", text );
		ImGui.PopFont();
	}

	private static void Caption( string text )
	{
		ImGui.PushStyleColor( ImGuiCol.Text, AdminMenuTheme.Muted );
		ImGui.TextWrapped( "{0}", text );
		ImGui.PopStyleColor();
	}

	private static void Centered( string text, Vector4 color, float size = 0 )
	{
		if ( size > 0 ) ImGui.PushFont( null, size );
		var width = ImGui.CalcTextSize( text ).x;
		ImGui.SetCursorPosX( ImGui.GetCursorPosX() + MathF.Max( 0, (ImGui.GetContentRegionAvail().x - width) / 2 ) );
		ImGui.TextColored( color, "{0}", text );
		if ( size > 0 ) ImGui.PopFont();
	}

	private static float ButtonWidth( string label ) => ImGui.CalcTextSize( label, true ).x + ImGui.GetStyle().FramePadding.x * 2;

	private Vector2 BadgeSize( string text )
	{
		ImGui.PushFont( null, AdminMenuTheme.SmallSize );
		var size = ImGui.CalcTextSize( text ) + new Vector2( 16, 6 ) * Scale;
		ImGui.PopFont();
		return size;
	}

	/// <summary>Rounded status pill; it is layout-only and never interactive.</summary>
	private void Badge( string text, Vector4 tone )
	{
		var size = BadgeSize( text );
		var min = ImGui.GetCursorScreenPos();
		var max = min + size;
		var draw = ImGui.GetWindowDrawList();
		draw.AddRectFilled( min, max, ImGui.GetColorU32( AdminMenuTheme.Fade( tone, 0.16f ) ), size.y / 2 );
		draw.AddRect( min, max, ImGui.GetColorU32( AdminMenuTheme.Fade( tone, 0.5f ) ), size.y / 2 );
		ImGui.PushFont( null, AdminMenuTheme.SmallSize );
		draw.AddText( min + new Vector2( 8, 3 ) * Scale, ImGui.GetColorU32( tone ), text );
		ImGui.PopFont();
		ImGui.Dummy( size );
	}

	/// <summary>Lays badges left to right and wraps them inside the current column.</summary>
	private void Badges( IEnumerable<(string Text, Vector4 Tone)> badges )
	{
		var first = true;
		foreach ( var (text, tone) in badges )
		{
			if ( !first )
			{
				var spacing = ImGui.GetStyle().ItemSpacing.x * 0.6f;
				var right = ImGui.GetItemRectMax().x + spacing + BadgeSize( text ).x;
				if ( right <= ImGui.GetWindowPos().x + ImGui.GetWindowContentRegionMax().x ) ImGui.SameLine( 0, spacing );
			}
			Badge( text, tone );
			first = false;
		}
	}

	/// <summary>Tinted button whose color states the consequence of the action.</summary>
	private static bool ToneButton( string label, Vector4 tone, Vector2 size = default, bool solid = false )
	{
		ImGui.PushStyleColor( ImGuiCol.Button, solid ? tone : AdminMenuTheme.Fade( tone, 0.16f ) );
		ImGui.PushStyleColor( ImGuiCol.ButtonHovered, solid ? AdminMenuTheme.Fade( tone, 0.85f ) : AdminMenuTheme.Fade( tone, 0.3f ) );
		ImGui.PushStyleColor( ImGuiCol.ButtonActive, solid ? AdminMenuTheme.Fade( tone, 0.7f ) : AdminMenuTheme.Fade( tone, 0.42f ) );
		ImGui.PushStyleColor( ImGuiCol.Border, AdminMenuTheme.Fade( tone, 0.6f ) );
		var pressed = ImGui.Button( label, size );
		ImGui.PopStyleColor( 4 );
		return pressed;
	}

	/// <summary>Full-width message strip with a colored edge.</summary>
	private void Banner( string text, Vector4 tone )
	{
		var scale = Scale;
		var pad = new Vector2( 14, 9 ) * scale;
		var edge = 4 * scale;
		var width = ImGui.GetContentRegionAvail().x;
		var wrap = MathF.Max( 1, width - pad.x * 2 - edge );
		var size = new Vector2( width, ImGui.CalcTextSize( text, false, wrap ).y + pad.y * 2 );
		var min = ImGui.GetCursorScreenPos();
		var draw = ImGui.GetWindowDrawList();
		var rounding = ImGui.GetStyle().FrameRounding;
		draw.AddRectFilled( min, min + size, ImGui.GetColorU32( AdminMenuTheme.Fade( tone, 0.12f ) ), rounding );
		// Inset the edge so its narrow radius stays inside the fill's rounded corners.
		draw.AddRectFilled( new Vector2( min.x, min.y + rounding / 2 ), new Vector2( min.x + edge, min.y + size.y - rounding / 2 ), ImGui.GetColorU32( tone ), edge / 2 );
		draw.AddText( 0, min + new Vector2( pad.x + edge, pad.y ), ImGui.GetColorU32( AdminMenuTheme.Text ), text, wrap );
		ImGui.Dummy( size );
	}

	/// <summary>Hinted single-line input that keeps the model's length bound.</summary>
	private static bool Field( string id, string hint, ref string text, int maxLength, float width, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		ImGui.SetNextItemWidth( width );
		return ImGui.InputTextEx( id, hint, ref text, maxLength, Vector2.Zero, flags, null );
	}

	private static string Utc( DateTimeOffset value ) => value.UtcDateTime.ToString( "yyyy-MM-dd HH:mm" );
}
