using Sandbox;
using SboxSuperuser.Core.Menu;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const ImGuiWindowFlags NoticeFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize
		| ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoBringToFrontOnFocus;

	private readonly Notices<(string Text, Vector4 Color)> _notices = new();

	/// <summary>Shows a result briefly in the bottom-right corner, as well as on the menu bar.</summary>
	private void Notify( string text, Vector4 color ) => _notices.Add( (text, color), DateTimeOffset.UtcNow );

	private void DrawNotices()
	{
		var visible = _notices.Visible( DateTimeOffset.UtcNow );
		if ( visible.Count == 0 ) return;

		var margin = 16 * Scale;
		ImGui.SetNextWindowPos( new Vector2( Screen.Width - margin, Screen.Height - margin ), ImGuiCond.Always, new Vector2( 1, 1 ) );
		ImGui.SetNextWindowBgAlpha( 0.9f * visible.Max( notice => notice.Opacity ) );
		if ( ImGui.Begin( "##notices", NoticeFlags ) )
		{
			ImGui.PushTextWrapPos( 380 * Scale );
			foreach ( var ((text, color), opacity) in visible )
				ImGui.TextColored( new Vector4( color.x, color.y, color.z, color.w * opacity ), "{0}", text );
			ImGui.PopTextWrapPos();
		}
		ImGui.End();
	}
}
