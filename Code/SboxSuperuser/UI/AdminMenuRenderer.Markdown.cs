using Sandbox;
using SboxSuperuser.Core.Documentation;
using SboxSuperuser.Core.Effects;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const int MarkdownCacheSize = 16;
	private readonly Dictionary<string, List<HighlightSpan>> _effectLines = new( StringComparer.Ordinal );
	private static readonly Vector4 CodeColor = new( 0.95f, 0.75f, 0.45f, 1 );
	private static readonly Vector4 BoldColor = new( 1, 1, 1, 1 );

	// Pages are parsed once and reused while they are on screen.
	private readonly Dictionary<string, IReadOnlyList<MarkdownBlock>> _markdownCache = new( StringComparer.Ordinal );

	/// <summary>Draws a Markdown page in the current window, wrapping to its width.</summary>
	private void DrawMarkdown( string markdown )
	{
		if ( !_markdownCache.TryGetValue( markdown, out var blocks ) )
		{
			if ( _markdownCache.Count >= MarkdownCacheSize ) _markdownCache.Clear();
			blocks = MarkdownParser.Parse( markdown );
			_markdownCache[ markdown ] = blocks;
		}

		for ( var index = 0; index < blocks.Count; index++ )
		{
			ImGui.PushID( index );
			DrawMarkdownBlock( blocks[ index ] );
			ImGui.PopID();
		}
	}

	private void DrawMarkdownBlock( MarkdownBlock block )
	{
		switch ( block.Kind )
		{
			case MarkdownBlockKind.Heading:
				ImGui.Spacing();
				ImGui.PushFont( null, block.Level switch { 1 => 22, 2 => 18, _ => 15 } );
				ImGui.TextUnformatted( string.Concat( block.Spans.Select( span => span.Text ) ) );
				ImGui.PopFont();
				if ( block.Level <= 2 ) ImGui.Separator();
				break;
			case MarkdownBlockKind.Paragraph:
				DrawInline( block.Spans );
				ImGui.Spacing();
				break;
			case MarkdownBlockKind.BulletList:
			case MarkdownBlockKind.NumberedList:
				for ( var item = 0; item < block.Items.Count; item++ )
				{
					if ( block.Kind == MarkdownBlockKind.BulletList ) ImGui.Bullet();
					else ImGui.TextDisabled( "{0}.", item + 1 );
					ImGui.SameLine();
					DrawInline( block.Items[ item ] );
				}
				ImGui.Spacing();
				break;
			case MarkdownBlockKind.Code:
				DrawCodeBlock( block.Code, block.Language );
				break;
			case MarkdownBlockKind.Table:
				DrawMarkdownTable( block );
				break;
			case MarkdownBlockKind.Rule:
				ImGui.Separator();
				break;
		}
	}

	/// <summary>Draws styled runs word by word, wrapping at the right edge and keeping the starting indent.</summary>
	private static void DrawInline( IReadOnlyList<MarkdownSpan> spans )
	{
		var startX = ImGui.GetCursorPosX();
		var right = ImGui.GetContentRegionMax().x;
		var first = true;
		foreach ( var span in spans )
		{
			foreach ( var word in Words( span ) )
			{
				var width = ImGui.CalcTextSize( word ).x;
				if ( !first )
				{
					ImGui.SameLine( 0, 0 );
					if ( ImGui.GetCursorPosX() + width > right && ImGui.GetCursorPosX() > startX )
					{
						ImGui.NewLine();
						ImGui.SetCursorPosX( startX );
					}
				}
				DrawWord( word, span.Style );
				first = false;
			}
		}
		if ( first ) ImGui.NewLine();
	}

	// Words keep their trailing space so spacing survives wrapping; inline code stays in one piece.
	private static IEnumerable<string> Words( MarkdownSpan span )
	{
		if ( span.Style == MarkdownSpanStyle.Code )
		{
			yield return span.Text;
			yield break;
		}
		var start = 0;
		for ( var index = 0; index < span.Text.Length; index++ )
		{
			if ( span.Text[ index ] != ' ' ) continue;
			yield return span.Text.Substring( start, index - start + 1 );
			start = index + 1;
		}
		if ( start < span.Text.Length ) yield return span.Text.Substring( start );
	}

	private static void DrawWord( string word, MarkdownSpanStyle style )
	{
		switch ( style )
		{
			case MarkdownSpanStyle.Code:
				var position = ImGui.GetCursorScreenPos();
				var size = ImGui.CalcTextSize( word );
				ImGui.GetWindowDrawList().AddRectFilled( position - new Vector2( 2, 0 ), position + size + new Vector2( 2, 0 ),
					ImGui.GetColorU32( ImGuiCol.FrameBg ), 3 );
				ImGui.TextColored( CodeColor, "{0}", word );
				break;
			case MarkdownSpanStyle.Bold:
				ImGui.TextColored( BoldColor, "{0}", word );
				break;
			case MarkdownSpanStyle.Italic:
				ImGui.TextDisabled( "{0}", word );
				break;
			default:
				ImGui.TextUnformatted( word );
				break;
		}
	}

	/// <summary>A framed, scrollable code block with a Copy button; effect scripts are syntax highlighted.</summary>
	private void DrawCodeBlock( string code, string language )
	{
		var lines = code.Split( '\n' );
		var style = ImGui.GetStyle();
		var height = lines.Length * ImGui.GetTextLineHeightWithSpacing() + style.WindowPadding.y * 2;
		ImGui.PushStyleColor( ImGuiCol.ChildBg, style[ ImGuiCol.FrameBg ] );
		ImGui.BeginChild( "code", new Vector2( 0, height ), ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding,
			ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoScrollWithMouse );
		foreach ( var line in lines ) DrawCodeLine( line, language );
		ImGui.EndChild();
		ImGui.PopStyleColor();
		if ( ImGui.SmallButton( "Copy" ) ) Sandbox.UI.Clipboard.SetText( code );
		ImGui.Spacing();
	}

	private void DrawMarkdownTable( MarkdownBlock block )
	{
		var columns = Math.Max( 1, block.Items.Count );
		if ( !ImGui.BeginTable( "table", columns, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp ) ) return;
		// Header text comes from the page, so it is drawn as text rather than used as column labels (which are IDs).
		for ( var column = 0; column < columns; column++ ) ImGui.TableSetupColumn( $"##c{column}" );
		ImGui.TableNextRow();
		foreach ( var header in block.Items )
		{
			ImGui.TableNextColumn();
			ImGui.TextColored( BoldColor, "{0}", string.Concat( header.Select( span => span.Text ) ) );
		}
		foreach ( var row in block.Rows )
		{
			ImGui.TableNextRow();
			for ( var column = 0; column < columns; column++ )
			{
				ImGui.TableNextColumn();
				if ( column < row.Count ) DrawInline( row[ column ] );
			}
		}
		ImGui.EndTable();
		ImGui.Spacing();
	}

	// Part B replaces this with effect-script highlighting for "effect" blocks.
	// Effect script lines are colored by the effect tokenizer; anything else is drawn plain.
	private void DrawCodeLine( string line, string language )
	{
		if ( language != "effect" || line.Length == 0 )
		{
			ImGui.TextUnformatted( line.Length == 0 ? " " : line );
			return;
		}
		if ( !_effectLines.TryGetValue( line, out var spans ) )
		{
			if ( _effectLines.Count >= 256 ) _effectLines.Clear();
			spans = EffectHighlighter.Spans( line, EffectLexer.Tokenize( line, new List<EffectDiagnostic>() ) );
			_effectLines[ line ] = spans;
		}
		var start = ImGui.GetCursorScreenPos();
		var draw = ImGui.GetWindowDrawList();
		foreach ( var span in spans )
		{
			var x = span.Column == 0 ? 0 : ImGui.CalcTextSize( line.Substring( 0, span.Column ) ).x;
			draw.AddText( start + new Vector2( x, 0 ), ImGui.GetColorU32( span.Color ), line.Substring( span.Column, span.Length ) );
		}
		ImGui.Dummy( new Vector2( ImGui.CalcTextSize( line ).x, ImGui.GetTextLineHeight() ) );
	}
}
