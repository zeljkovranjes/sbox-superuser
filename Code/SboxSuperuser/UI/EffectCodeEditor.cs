using Sandbox;
using SboxSuperuser.Core.Editing;
using SboxSuperuser.Core.Effects;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

/// <summary>
/// Superuser's code editor for effect scripts: numbered lines, coloring from the effect tokenizer, color swatches,
/// error underlines, selection, clipboard, undo and auto-indent. The text logic lives in TextBuffer (Core); this
/// class only draws it and feeds it keys.
/// </summary>
internal sealed class EffectCodeEditor
{
	private static readonly Vector4 GutterText = new( 0.45f, 0.48f, 0.53f, 1 );
	private static readonly Vector4 ErrorColor = new( 0.96f, 0.28f, 0.28f, 1 );
	private static readonly Vector4 SelectionColor = new( 0.15f, 0.31f, 0.52f, 1 );
	private static readonly Vector4 CursorColor = new( 0.95f, 0.95f, 0.95f, 1 );

	private readonly List<Piece> _pieces = new();
	private EffectCompilation _compilation;
	private int _compiledRevision = -1;
	private int _laidOutRevision = -1;
	private string[] _lines = { "" };
	private float _widest;
	private bool _dragging;
	private bool _scrollToCursor;
	private Func<bool, string> _previousClipboard;

	internal EffectCodeEditor( string text )
	{
		Buffer = new TextBuffer( text, EffectProgram.MaximumSourceLength );
	}

	internal TextBuffer Buffer { get; }

	/// <summary>The latest compile of the text, refreshed after every edit.</summary>
	internal EffectCompilation Compilation
	{
		get
		{
			if ( _compiledRevision == Buffer.Revision ) return _compilation;
			_compilation = EffectCompiler.Compile( Buffer.Text );
			_compiledRevision = Buffer.Revision;
			return _compilation;
		}
	}

	internal bool Focused { get; private set; }

	/// <summary>Takes or releases the keyboard; while focused, Ctrl+C and Ctrl+X copy from the editor.</summary>
	internal void SetFocused( bool focused )
	{
		if ( Focused == focused ) return;
		Focused = focused;
		if ( focused )
		{
			_previousClipboard = ImGuiHost.ClipboardCopyProvider;
			ImGuiHost.ClipboardCopyProvider = Copy;
		}
		else if ( ImGuiHost.ClipboardCopyProvider == Copy )
		{
			ImGuiHost.ClipboardCopyProvider = _previousClipboard;
		}
	}

	/// <summary>Replaces the text (a template or preset), keeping it undoable, and takes focus.</summary>
	internal void Load( string text )
	{
		Buffer.SetText( text );
		Buffer.DocumentEdge( false, false );
		SetFocused( true );
		_scrollToCursor = true;
	}

	/// <summary>Types text at the cursor, as if the user had typed it.</summary>
	internal void InsertAtCursor( string text )
	{
		Buffer.Insert( text );
		SetFocused( true );
		_scrollToCursor = true;
	}

	internal void Draw( string id, Vector2 size )
	{
		if ( Buffer.Revision != _laidOutRevision ) Layout();
		var style = ImGui.GetStyle();
		ImGui.PushStyleColor( ImGuiCol.ChildBg, style[ ImGuiCol.FrameBg ] );
		ImGui.BeginChild( id, size, ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar );

		var lineHeight = ImGui.GetTextLineHeight() + 3;
		var gutter = ImGui.CalcTextSize( "0000" ).x + 14;
		var origin = ImGui.GetCursorScreenPos();
		var available = ImGui.GetContentRegionAvail();
		var area = new Vector2( MathF.Max( _widest + gutter + 24, available.x ), MathF.Max( _lines.Length * lineHeight + lineHeight, available.y ) );
		ImGui.InvisibleButton( "##code", area );
		var hovered = ImGui.IsItemHovered();
		HandleMouse( origin, gutter, lineHeight, hovered );
		if ( Focused )
		{
			HandleKeys( lineHeight );
			// Tells the input panel that keys are text now, so the menu's open key types instead of closing it.
			ImGuiContext.Current.WantTextInputNextFrame = 1;
		}
		if ( Buffer.Revision != _laidOutRevision ) Layout();

		var scroll = ImGui.GetScrollY();
		var height = ImGui.GetWindowHeight();
		var first = Math.Max( 0, (int)(scroll / lineHeight) - 1 );
		var last = Math.Min( _lines.Length - 1, (int)((scroll + height) / lineHeight) + 1 );
		var draw = ImGui.GetWindowDrawList();
		DrawGutter( draw, origin, gutter, lineHeight, first, last, scroll, height );
		DrawSelection( draw, origin, gutter, lineHeight, first, last );
		DrawText( draw, origin, gutter, lineHeight, first, last );
		DrawDiagnostics( draw, origin, gutter, lineHeight );
		if ( Focused && ImGui.GetTime() % 1.0 < 0.6 ) DrawCursor( draw, origin, gutter, lineHeight );
		if ( _scrollToCursor ) ScrollToCursor( lineHeight, scroll, height );
		ImGui.EndChild();
		ImGui.PopStyleColor();
	}

	// Measures every colored piece once per edit; needs a live ImGui frame for the font.
	private void Layout()
	{
		var compilation = Compilation;
		_laidOutRevision = Buffer.Revision;
		_lines = Buffer.Text.Split( '\n' );
		_pieces.Clear();
		foreach ( var span in EffectHighlighter.Spans( compilation.Source, compilation.Tokens ) )
		{
			var line = _lines[ span.Line ];
			var text = line.Substring( span.Column, Math.Min( span.Length, line.Length - span.Column ) );
			_pieces.Add( new Piece( span.Line, text, Measure( line, span.Column ), ImGui.CalcTextSize( text ).x, span.Color, span.Swatch ) );
		}
		_widest = _lines.Length == 0 ? 0 : _lines.Max( line => ImGui.CalcTextSize( line ).x );
	}

	private void HandleMouse( Vector2 origin, float gutter, float lineHeight, bool hovered )
	{
		var mouse = ImGui.GetMousePos();
		if ( ImGui.IsItemClicked() )
		{
			SetFocused( true );
			var offset = OffsetAt( mouse, origin, gutter, lineHeight );
			if ( ImGui.IsMouseDoubleClicked( ImGuiMouseButton.Left ) ) Buffer.SelectWord( offset );
			else Buffer.MoveTo( offset, ImGui.GetIO().KeyShift );
			_dragging = true;
		}
		else if ( ImGui.IsMouseClicked( ImGuiMouseButton.Left ) && !hovered && !ImGui.IsWindowHovered() )
		{
			SetFocused( false );
		}

		if ( !ImGui.IsMouseDown( ImGuiMouseButton.Left ) ) _dragging = false;
		else if ( _dragging && !ImGui.IsMouseClicked( ImGuiMouseButton.Left ) )
		{
			Buffer.MoveTo( OffsetAt( mouse, origin, gutter, lineHeight ), true );
			_scrollToCursor = true;
		}
	}

	private void HandleKeys( float lineHeight )
	{
		var io = ImGui.GetIO();
		var page = Math.Max( 1, (int)(ImGui.GetWindowHeight() / lineHeight) - 1 );
		foreach ( var key in io.InputQueueKeys )
		{
			_scrollToCursor = true;
			switch ( key.Key )
			{
				case ImGuiKey.LeftArrow: Buffer.MoveHorizontal( -1, key.Ctrl, key.Shift ); break;
				case ImGuiKey.RightArrow: Buffer.MoveHorizontal( 1, key.Ctrl, key.Shift ); break;
				case ImGuiKey.UpArrow: Buffer.MoveVertical( -1, key.Shift ); break;
				case ImGuiKey.DownArrow: Buffer.MoveVertical( 1, key.Shift ); break;
				case ImGuiKey.PageUp: Buffer.MoveVertical( -page, key.Shift ); break;
				case ImGuiKey.PageDown: Buffer.MoveVertical( page, key.Shift ); break;
				case ImGuiKey.Home when key.Ctrl: Buffer.DocumentEdge( false, key.Shift ); break;
				case ImGuiKey.Home: Buffer.Home( key.Shift ); break;
				case ImGuiKey.End when key.Ctrl: Buffer.DocumentEdge( true, key.Shift ); break;
				case ImGuiKey.End: Buffer.End( key.Shift ); break;
				case ImGuiKey.Backspace: Buffer.Backspace( key.Ctrl ); break;
				case ImGuiKey.Delete: Buffer.Delete( key.Ctrl ); break;
				case ImGuiKey.Enter:
				case ImGuiKey.KeypadEnter: Buffer.NewLine(); break;
				case ImGuiKey.Tab: Buffer.Tab( key.Shift ); break;
				case ImGuiKey.Escape: SetFocused( false ); break;
				case ImGuiKey.A when key.Ctrl: Buffer.SelectAll(); break;
				case ImGuiKey.Z when key.Ctrl && key.Shift: Buffer.Redo(); break;
				case ImGuiKey.Z when key.Ctrl: Buffer.Undo(); break;
				case ImGuiKey.Y when key.Ctrl: Buffer.Redo(); break;
				default: _scrollToCursor = false; break;
			}
		}
		io.InputQueueKeys.Clear();

		if ( !io.KeyCtrl )
		{
			foreach ( var character in io.InputQueueCharacters )
			{
				if ( character == '}' ) Buffer.CloseBrace();
				else Buffer.Insert( character.ToString() );
				_scrollToCursor = true;
			}
		}
		io.InputQueueCharacters.Clear();

		if ( io.PastedText is { } pasted )
		{
			Buffer.Insert( pasted );
			io.PastedText = null;
			_scrollToCursor = true;
		}
	}

	// Called when the game asks for clipboard text on Ctrl+C or Ctrl+X.
	private string Copy( bool cut )
	{
		if ( !Focused || !Buffer.HasSelection ) return _previousClipboard?.Invoke( cut );
		return cut ? Buffer.Cut() : Buffer.SelectedText;
	}

	private void DrawGutter( ImDrawList draw, Vector2 origin, float gutter, float lineHeight, int first, int last, float scroll, float height )
	{
		draw.AddRectFilled( new Vector2( origin.x, origin.y + scroll - 4 ), new Vector2( origin.x + gutter - 6, origin.y + scroll + height ),
			ImGui.GetColorU32( ImGuiCol.WindowBg, 0.6f ) );
		var errorLines = Compilation.Diagnostics.Select( diagnostic => diagnostic.Line( Compilation.Source ) - 1 ).ToHashSet();
		var cursorLine = Buffer.LineOf( Buffer.Cursor );
		for ( var line = first; line <= last; line++ )
		{
			var number = (line + 1).ToString();
			var color = errorLines.Contains( line ) ? ErrorColor : line == cursorLine && Focused ? CursorColor : GutterText;
			draw.AddText( new Vector2( origin.x + gutter - 12 - ImGui.CalcTextSize( number ).x, origin.y + line * lineHeight + 1 ),
				ImGui.GetColorU32( color ), number );
		}
	}

	private void DrawSelection( ImDrawList draw, Vector2 origin, float gutter, float lineHeight, int first, int last )
	{
		if ( !Buffer.HasSelection ) return;
		var startLine = Buffer.LineOf( Buffer.SelectionStart );
		var endLine = Buffer.LineOf( Buffer.SelectionEnd );
		var color = ImGui.GetColorU32( SelectionColor );
		for ( var line = Math.Max( first, startLine ); line <= Math.Min( last, endLine ); line++ )
		{
			var from = line == startLine ? Buffer.ColumnOf( Buffer.SelectionStart ) : 0;
			var to = line == endLine ? Buffer.ColumnOf( Buffer.SelectionEnd ) : _lines[ line ].Length;
			var left = origin.x + gutter + Measure( _lines[ line ], from );
			var right = origin.x + gutter + Measure( _lines[ line ], to ) + (line == endLine ? 0 : 6);
			var top = origin.y + line * lineHeight;
			draw.AddRectFilled( new Vector2( left, top ), new Vector2( right, top + lineHeight ), color );
		}
	}

	private void DrawText( ImDrawList draw, Vector2 origin, float gutter, float lineHeight, int first, int last )
	{
		foreach ( var piece in _pieces )
		{
			if ( piece.Line < first || piece.Line > last ) continue;
			var position = new Vector2( origin.x + gutter + piece.X, origin.y + piece.Line * lineHeight + 1 );
			draw.AddText( position, ImGui.GetColorU32( piece.Color ), piece.Text );
			if ( piece.Swatch is { } swatch )
			{
				var bottom = origin.y + (piece.Line + 1) * lineHeight;
				draw.AddRectFilled( new Vector2( position.x, bottom - 3 ), new Vector2( position.x + piece.Width, bottom - 1 ), ImGui.GetColorU32( swatch ) );
			}
		}
	}

	private void DrawDiagnostics( ImDrawList draw, Vector2 origin, float gutter, float lineHeight )
	{
		var starts = EffectHighlighter.LineStarts( Compilation.Source );
		var color = ImGui.GetColorU32( ErrorColor );
		foreach ( var diagnostic in Compilation.Diagnostics )
		{
			var line = EffectHighlighter.LineOf( starts, Math.Min( diagnostic.Start, Compilation.Source.Length ) );
			if ( line >= _lines.Length ) continue;
			var text = _lines[ line ];
			var column = Math.Min( diagnostic.Start - starts[ line ], text.Length );
			var end = Math.Min( column + diagnostic.Length, text.Length );
			var left = origin.x + gutter + Measure( text, column );
			var right = MathF.Max( left + 6, origin.x + gutter + Measure( text, end ) );
			var y = origin.y + (line + 1) * lineHeight - 1;
			draw.AddLine( new Vector2( left, y ), new Vector2( right, y ), color, 2 );
		}
	}

	private void DrawCursor( ImDrawList draw, Vector2 origin, float gutter, float lineHeight )
	{
		var line = Buffer.LineOf( Buffer.Cursor );
		var x = origin.x + gutter + Measure( _lines[ line ], Buffer.ColumnOf( Buffer.Cursor ) );
		var top = origin.y + line * lineHeight;
		draw.AddLine( new Vector2( x, top + 1 ), new Vector2( x, top + lineHeight - 1 ), ImGui.GetColorU32( CursorColor ), 1.5f );
	}

	private void ScrollToCursor( float lineHeight, float scroll, float height )
	{
		_scrollToCursor = false;
		var top = Buffer.LineOf( Buffer.Cursor ) * lineHeight;
		if ( top < scroll ) ImGui.SetScrollY( top );
		else if ( top + lineHeight * 2 > scroll + height ) ImGui.SetScrollY( top + lineHeight * 2 - height );
	}

	private int OffsetAt( Vector2 mouse, Vector2 origin, float gutter, float lineHeight )
	{
		var line = Math.Clamp( (int)MathF.Floor( (mouse.y - origin.y) / lineHeight ), 0, _lines.Length - 1 );
		var x = mouse.x - origin.x - gutter;
		var text = _lines[ line ];
		var column = 0;
		var width = 0f;
		while ( column < text.Length )
		{
			var next = ImGui.CalcTextSize( text[ column ].ToString() ).x;
			if ( width + next / 2 > x ) break;
			width += next;
			column++;
		}
		return Buffer.OffsetAt( line, column );
	}

	private static float Measure( string line, int column ) => column <= 0 ? 0 : ImGui.CalcTextSize( line.Substring( 0, Math.Min( column, line.Length ) ) ).x;

	private readonly record struct Piece( int Line, string Text, float X, float Width, Vector4 Color, Vector4? Swatch );
}
