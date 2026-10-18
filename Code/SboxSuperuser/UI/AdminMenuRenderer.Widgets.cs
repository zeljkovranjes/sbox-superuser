using Sandbox;
using SboxSuperuser.Core.Menu;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

// Every host- or player-supplied string is drawn as text or a "{0}" argument, never as an ImGui ID or format string.
internal sealed partial class AdminMenuRenderer
{
	private float Scale => _context.AppliedStyleScale;

	private static void Caption( string text )
	{
		ImGui.PushStyleColor( ImGuiCol.Text, ImGui.GetStyle()[ ImGuiCol.TextDisabled ] );
		ImGui.TextWrapped( "{0}", text );
		ImGui.PopStyleColor();
	}

	private static void ColoredWrapped( string text, Vector4 color )
	{
		ImGui.PushStyleColor( ImGuiCol.Text, color );
		ImGui.TextWrapped( "{0}", text );
		ImGui.PopStyleColor();
	}

	/// <summary>Previous/next buttons around a page number; returns -1, 0 or +1 for the page change.</summary>
	private static int Pager( string id, int page, bool hasMore, string previous = "Previous", string next = "Next" )
	{
		var change = 0;
		ImGui.PushID( id );
		ImGui.BeginDisabled( page == 0 );
		if ( ImGui.Button( previous ) && page > 0 ) change = -1;
		ImGui.EndDisabled();
		ImGui.SameLine();
		ImGui.AlignTextToFramePadding();
		ImGui.TextDisabled( "Page {0}", page + 1 );
		ImGui.SameLine();
		ImGui.BeginDisabled( !hasMore );
		if ( ImGui.Button( next ) ) change = 1;
		ImGui.EndDisabled();
		ImGui.PopID();
		return change;
	}

	/// <summary>Hinted single-line input that keeps the model's length bound.</summary>
	private static bool Field( string id, string hint, ref string text, int maxLength, float width, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		ImGui.SetNextItemWidth( width );
		return ImGui.InputTextEx( id, hint, ref text, maxLength, Vector2.Zero, flags, null );
	}

	private static float ButtonWidth( string label ) => ImGui.CalcTextSize( label, true ).x + ImGui.GetStyle().FramePadding.x * 2;

	private static string Utc( DateTimeOffset value ) => value.UtcDateTime.ToString( "yyyy-MM-dd HH:mm" );

	/// <summary>"in 2h 14m" for an active restriction, with the exact UTC time on hover; past ones show the date.</summary>
	private static void DrawExpiry( DateTimeOffset? expires, bool active )
	{
		if ( expires is not { } time )
		{
			ImGui.TextUnformatted( "Permanent" );
			return;
		}
		if ( !active )
		{
			ImGui.TextUnformatted( Utc( time ) );
			return;
		}
		ImGui.TextUnformatted( "in " + TimeRemaining.Format( time - DateTimeOffset.UtcNow ) );
		ImGui.SetItemTooltip( "{0} UTC", Utc( time ) );
	}
}
