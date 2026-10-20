using SboxSuperuser.Core.Editing;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class TextBufferTests
{
	[Fact]
	public void TypingUndoesAsOneStep()
	{
		var buffer = new TextBuffer();
		foreach ( var letter in "red" ) buffer.Insert( letter.ToString() );

		buffer.Undo();

		Assert.Equal( "", buffer.Text );
		buffer.Redo();
		Assert.Equal( "red", buffer.Text );
	}

	[Fact]
	public void NewLinesKeepTheIndentAndOpenBracesAddOne()
	{
		var buffer = new TextBuffer( "if i == 0 {" );
		buffer.DocumentEdge( true, false );

		buffer.NewLine();
		buffer.Insert( "color = red" );
		buffer.NewLine();
		buffer.CloseBrace();

		Assert.Equal( "if i == 0 {\n    color = red\n}", buffer.Text );
	}

	[Fact]
	public void BackspaceInIndentRemovesAWholeStep()
	{
		var buffer = new TextBuffer( "        x" );
		buffer.MoveTo( 8, false );

		buffer.Backspace( false );

		Assert.Equal( "    x", buffer.Text );
	}

	[Fact]
	public void TabIndentsSelectedLinesAndShiftTabUndoesIt()
	{
		var buffer = new TextBuffer( "a\nb\nc" );
		buffer.SelectAll();

		buffer.Tab( false );
		Assert.Equal( "    a\n    b\n    c", buffer.Text );
		buffer.Tab( true );
		Assert.Equal( "a\nb\nc", buffer.Text );
	}

	[Fact]
	public void TabsBecomeSpacesAndControlCharactersAreDropped()
	{
		Assert.Equal( "    a\nb", new TextBuffer( "\ta\r\nb\u0007" ).Text );
	}

	[Fact]
	public void WordJumpsStopAtWordsAndSymbols()
	{
		var buffer = new TextBuffer( "color = mix(red, blue)" );

		buffer.MoveHorizontal( 1, true, false );
		Assert.Equal( 6, buffer.Cursor );
		buffer.DocumentEdge( true, false );
		buffer.MoveHorizontal( -1, true, false );
		Assert.Equal( 21, buffer.Cursor );
		buffer.MoveHorizontal( -1, true, true );
		Assert.Equal( "blue", buffer.SelectedText );
	}

	[Fact]
	public void UpAndDownKeepTheColumn()
	{
		var buffer = new TextBuffer( "abcdef\nab\nabcdef" );
		buffer.MoveTo( 5, false );

		buffer.MoveVertical( 1, false );
		Assert.Equal( (1, 2), (buffer.LineOf( buffer.Cursor ), buffer.ColumnOf( buffer.Cursor )) );
		buffer.MoveVertical( 1, false );
		Assert.Equal( (2, 5), (buffer.LineOf( buffer.Cursor ), buffer.ColumnOf( buffer.Cursor )) );
	}

	[Fact]
	public void TheLengthLimitHolds()
	{
		var buffer = new TextBuffer( "abc", maximumLength: 4 );
		buffer.DocumentEdge( true, false );

		Assert.True( buffer.Insert( "d" ) );
		Assert.False( buffer.Insert( "e" ) );
		Assert.Equal( "abcd", buffer.Text );
	}

	[Fact]
	public void CutRemovesTheSelection()
	{
		var buffer = new TextBuffer( "color = red" );
		buffer.SelectWord( 9 );

		Assert.Equal( "red", buffer.Cut() );
		Assert.Equal( "color = ", buffer.Text );
	}
}
