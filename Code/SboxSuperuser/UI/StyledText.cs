using Sandbox;
using SboxSuperuser.Core.Prefixes;
using SboxSuperuser.Engine.Prefixes;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

/// <summary>Draws a rendered prefix line in ImGui at the cursor, then moves the cursor past it like text.</summary>
internal static class StyledText
{
	/// <summary>Draws the runs; shadow adds a thin dark copy behind them for text drawn straight over the game.</summary>
	internal static void Draw( IReadOnlyList<LineRun> runs, bool shadow = false )
	{
		var draw = ImGui.GetWindowDrawList();
		var start = ImGui.GetCursorScreenPos();
		var space = ImGui.CalcTextSize( " " ).x;
		var gaps = 0f;
		var drawn = "";
		foreach ( var run in runs )
		{
			gaps += run.Gap * space;
			// Each run starts where the whole line so far ends. Adding up run widths instead would shift the rest of
			// the line by a pixel whenever an effect moves a color boundary (kerning and rounding differ per split).
			var position = new Vector2( start.x + gaps + ImGui.CalcTextSize( drawn ).x, start.y );
			var color = ImGui.GetColorU32( EffectColors.ToColor( run.Color ) );
			if ( shadow ) draw.AddText( position + Vector2.One, ImGui.GetColorU32( new Vector4( 0, 0, 0, 0.85f * run.Color.A ) ), run.Text );
			draw.AddText( position, color, run.Text );
			// The font has no bold or italic face here: bold is drawn twice, italic gets a faint shadow. Neither adds
			// width, so a letter turning bold never pushes the name.
			if ( run.Bold ) draw.AddText( position + new Vector2( 1, 0 ), color, run.Text );
			if ( run.Italic ) draw.AddText( position + new Vector2( 1, -1 ), ImGui.GetColorU32( EffectColors.ToColor( run.Color.WithAlpha( run.Color.A * 0.35 ) ) ), run.Text );
			drawn += run.Text;
		}
		var width = gaps + ImGui.CalcTextSize( drawn ).x;
		ImGui.Dummy( new Vector2( MathF.Max( 1, width ), ImGui.GetTextLineHeight() ) );
	}
}
