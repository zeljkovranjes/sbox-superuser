namespace SboxSuperuser.Core.Editing;

/// <summary>
/// The text, cursor and selection behind the effect code editor, with undo and redo. Offsets count characters
/// from the start; the selection runs between <see cref="Anchor"/> and <see cref="Cursor"/>. Tabs become four spaces.
/// </summary>
internal sealed class TextBuffer
{
	internal const string Indent = "    ";
	private const int UndoLimit = 200;

	private readonly Stack<Snapshot> _undo = new();
	private readonly Stack<Snapshot> _redo = new();
	private bool _typing;
	private int _preferredColumn = -1;

	internal TextBuffer( string text = "", int maximumLength = 4096 )
	{
		MaximumLength = maximumLength;
		Text = Clean( text );
		if ( Text.Length > MaximumLength ) Text = Text.Substring( 0, MaximumLength );
	}

	internal string Text { get; private set; }
	internal int Cursor { get; private set; }
	internal int Anchor { get; private set; }
	internal int MaximumLength { get; }

	/// <summary>Counts every edit, so views can tell when to recompile.</summary>
	internal int Revision { get; private set; }

	internal bool HasSelection => Cursor != Anchor;
	internal int SelectionStart => Math.Min( Cursor, Anchor );
	internal int SelectionEnd => Math.Max( Cursor, Anchor );
	internal string SelectedText => Text.Substring( SelectionStart, SelectionEnd - SelectionStart );
	internal bool CanUndo => _undo.Count > 0;
	internal bool CanRedo => _redo.Count > 0;

	internal int LineCount => Text.Count( character => character == '\n' ) + 1;

	internal int LineOf( int offset )
	{
		var line = 0;
		for ( var index = 0; index < offset && index < Text.Length; index++ )
			if ( Text[ index ] == '\n' ) line++;
		return line;
	}

	internal int LineStart( int line )
	{
		var offset = 0;
		for ( var current = 0; current < line; current++ )
		{
			var next = Text.IndexOf( '\n', offset );
			if ( next < 0 ) return Text.Length;
			offset = next + 1;
		}
		return offset;
	}

	internal int LineEnd( int line )
	{
		var end = Text.IndexOf( '\n', LineStart( line ) );
		return end < 0 ? Text.Length : end;
	}

	internal string Line( int line ) => Text.Substring( LineStart( line ), LineEnd( line ) - LineStart( line ) );

	internal int ColumnOf( int offset ) => offset - LineStart( LineOf( offset ) );

	internal int OffsetAt( int line, int column )
	{
		line = Math.Clamp( line, 0, LineCount - 1 );
		return LineStart( line ) + Math.Clamp( column, 0, LineEnd( line ) - LineStart( line ) );
	}

	/// <summary>Replaces everything, as one undoable step.</summary>
	internal void SetText( string text )
	{
		Remember();
		Text = Clean( text );
		if ( Text.Length > MaximumLength ) Text = Text.Substring( 0, MaximumLength );
		Cursor = Anchor = Text.Length;
		Changed();
	}

	/// <summary>Types or pastes text over the selection. Returns false when it would pass the length limit.</summary>
	internal bool Insert( string text )
	{
		text = Clean( text );
		if ( text.Length == 0 && !HasSelection ) return true;
		if ( Text.Length - (SelectionEnd - SelectionStart) + text.Length > MaximumLength ) return false;
		var typing = text.Length == 1 && text != "\n" && text != " " && !HasSelection;
		if ( !typing || !_typing ) Remember();
		_typing = typing;
		var start = SelectionStart;
		Text = Text.Remove( start, SelectionEnd - start ).Insert( start, text );
		Cursor = Anchor = start + text.Length;
		Changed( keepTyping: typing );
		return true;
	}

	/// <summary>A new line that keeps the current indent, one step deeper after an opening brace.</summary>
	internal void NewLine()
	{
		var line = Line( LineOf( Cursor ) );
		var indent = new string( line.TakeWhile( character => character == ' ' ).ToArray() );
		var before = Text.Substring( LineStart( LineOf( Cursor ) ), ColumnOf( Cursor ) ).TrimEnd();
		if ( before.EndsWith( "{" ) ) indent += Indent;
		Insert( "\n" + indent );
	}

	/// <summary>Types a closing brace, first removing one indent step when the line holds only spaces.</summary>
	internal void CloseBrace()
	{
		var line = LineOf( Cursor );
		var before = Text.Substring( LineStart( line ), ColumnOf( Cursor ) );
		if ( !HasSelection && before.Length >= Indent.Length && before.All( character => character == ' ' ) )
		{
			Remember();
			Text = Text.Remove( Cursor - Indent.Length, Indent.Length );
			Cursor = Anchor = Cursor - Indent.Length;
			Changed();
		}
		Insert( "}" );
	}

	/// <summary>Tab: indents every selected line, or inserts spaces up to the next indent stop.</summary>
	internal void Tab( bool outdent )
	{
		if ( outdent || HasSelection && LineOf( SelectionStart ) != LineOf( SelectionEnd ) )
		{
			ShiftLines( outdent );
			return;
		}
		Insert( new string( ' ', Indent.Length - ColumnOf( Cursor ) % Indent.Length ) );
	}

	internal void Backspace( bool word )
	{
		if ( HasSelection ) { Insert( "" ); return; }
		if ( Cursor == 0 ) return;
		var column = ColumnOf( Cursor );
		var before = Text.Substring( Cursor - column, column );
		// Backspace in leading spaces removes a whole indent step.
		var count = word ? Cursor - WordLeft( Cursor )
			: before.Length > 0 && before.All( character => character == ' ' ) ? (column - 1) % Indent.Length + 1 : 1;
		Anchor = Cursor - count;
		Insert( "" );
	}

	internal void Delete( bool word )
	{
		if ( HasSelection ) { Insert( "" ); return; }
		if ( Cursor == Text.Length ) return;
		Anchor = word ? WordRight( Cursor ) : Cursor + 1;
		Insert( "" );
	}

	internal void MoveHorizontal( int direction, bool word, bool select )
	{
		var target = HasSelection && !select
			? (direction < 0 ? SelectionStart : SelectionEnd)
			: word ? (direction < 0 ? WordLeft( Cursor ) : WordRight( Cursor ))
			: Math.Clamp( Cursor + direction, 0, Text.Length );
		MoveTo( target, select );
	}

	internal void MoveVertical( int lines, bool select )
	{
		var column = _preferredColumn >= 0 ? _preferredColumn : ColumnOf( Cursor );
		var line = LineOf( Cursor ) + lines;
		var target = line < 0 ? 0 : line >= LineCount ? Text.Length : OffsetAt( line, column );
		MoveTo( target, select );
		_preferredColumn = column;
	}

	/// <summary>Home jumps to the first non-space letter, or to column 0 when already there.</summary>
	internal void Home( bool select )
	{
		var start = LineStart( LineOf( Cursor ) );
		var firstLetter = start;
		while ( firstLetter < Text.Length && Text[ firstLetter ] == ' ' ) firstLetter++;
		MoveTo( Cursor == firstLetter ? start : firstLetter, select );
	}

	internal void End( bool select ) => MoveTo( LineEnd( LineOf( Cursor ) ), select );

	internal void DocumentEdge( bool end, bool select ) => MoveTo( end ? Text.Length : 0, select );

	internal void MoveTo( int offset, bool select )
	{
		Cursor = Math.Clamp( offset, 0, Text.Length );
		if ( !select ) Anchor = Cursor;
		_typing = false;
		_preferredColumn = -1;
	}

	internal void SelectAll()
	{
		Anchor = 0;
		MoveTo( Text.Length, true );
	}

	/// <summary>Selects the word around an offset (double click).</summary>
	internal void SelectWord( int offset )
	{
		offset = Math.Clamp( offset, 0, Text.Length );
		var start = offset;
		var end = offset;
		while ( start > 0 && IsWord( Text[ start - 1 ] ) ) start--;
		while ( end < Text.Length && IsWord( Text[ end ] ) ) end++;
		Anchor = start;
		MoveTo( end, true );
	}

	/// <summary>Removes and returns the selection.</summary>
	internal string Cut()
	{
		var text = SelectedText;
		if ( text.Length > 0 ) Insert( "" );
		return text;
	}

	internal void Undo() => Swap( _undo, _redo );

	internal void Redo() => Swap( _redo, _undo );

	private void ShiftLines( bool outdent )
	{
		var first = LineOf( SelectionStart );
		var last = LineOf( SelectionEnd );
		if ( HasSelection && last > first && ColumnOf( SelectionEnd ) == 0 ) last--;
		var lines = Text.Split( '\n' );
		var changed = false;
		for ( var line = first; line <= last; line++ )
		{
			var before = lines[ line ];
			lines[ line ] = outdent ? before.Substring( Math.Min( Indent.Length, before.TakeWhile( c => c == ' ' ).Count() ) ) : Indent + before;
			changed |= lines[ line ] != before;
		}
		var result = string.Join( "\n", lines );
		if ( !changed || result.Length > MaximumLength ) return;
		Remember();
		Text = result;
		Anchor = LineStart( first );
		Cursor = LineEnd( last );
		Changed();
	}

	private int WordLeft( int offset )
	{
		while ( offset > 0 && char.IsWhiteSpace( Text[ offset - 1 ] ) ) offset--;
		if ( offset > 0 && !IsWord( Text[ offset - 1 ] ) ) return offset - 1;
		while ( offset > 0 && IsWord( Text[ offset - 1 ] ) ) offset--;
		return offset;
	}

	private int WordRight( int offset )
	{
		if ( offset < Text.Length && !IsWord( Text[ offset ] ) && !char.IsWhiteSpace( Text[ offset ] ) ) return offset + 1;
		while ( offset < Text.Length && IsWord( Text[ offset ] ) ) offset++;
		while ( offset < Text.Length && char.IsWhiteSpace( Text[ offset ] ) && Text[ offset ] != '\n' ) offset++;
		return offset;
	}

	private static bool IsWord( char character ) => char.IsLetterOrDigit( character ) || character == '_';

	private void Remember()
	{
		_undo.Push( new Snapshot( Text, Cursor, Anchor ) );
		_redo.Clear();
		if ( _undo.Count <= UndoLimit ) return;
		var kept = _undo.Take( UndoLimit ).Reverse().ToArray();
		_undo.Clear();
		foreach ( var snapshot in kept ) _undo.Push( snapshot );
	}

	private void Swap( Stack<Snapshot> from, Stack<Snapshot> to )
	{
		if ( from.Count == 0 ) return;
		to.Push( new Snapshot( Text, Cursor, Anchor ) );
		var snapshot = from.Pop();
		Text = snapshot.Text;
		Cursor = snapshot.Cursor;
		Anchor = snapshot.Anchor;
		Changed();
	}

	private void Changed( bool keepTyping = false )
	{
		Revision++;
		_typing = keepTyping;
		_preferredColumn = -1;
	}

	private static string Clean( string text )
	{
		var normalized = (text ?? "").Replace( "\r\n", "\n" ).Replace( '\r', '\n' ).Replace( "\t", Indent );
		return new string( normalized.Where( character => character == '\n' || !char.IsControl( character ) ).ToArray() );
	}

	private readonly record struct Snapshot( string Text, int Cursor, int Anchor );
}
