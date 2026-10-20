using System.Text;

namespace SboxSuperuser.Core.Documentation;

/// <summary>
/// Parses the Markdown subset module pages use: headings, paragraphs, bullet and numbered lists, fenced code,
/// tables, rules, and inline code, bold, italic and links. Anything else is kept as plain paragraph text, so a
/// page never fails to show.
/// </summary>
internal static class MarkdownParser
{
	internal static IReadOnlyList<MarkdownBlock> Parse( string markdown )
	{
		var lines = (markdown ?? "").Replace( "\r\n", "\n" ).Split( '\n' );
		var blocks = new List<MarkdownBlock>();
		var paragraph = new List<string>();

		void FlushParagraph()
		{
			if ( paragraph.Count == 0 ) return;
			blocks.Add( new MarkdownBlock( MarkdownBlockKind.Paragraph ) { Spans = ParseInline( string.Join( " ", paragraph ) ) } );
			paragraph.Clear();
		}

		for ( var index = 0; index < lines.Length; index++ )
		{
			var line = lines[ index ];
			var trimmed = line.Trim();

			if ( trimmed.StartsWith( "```", StringComparison.Ordinal ) )
			{
				FlushParagraph();
				index = ReadCode( lines, index, trimmed.Substring( 3 ).Trim(), blocks );
			}
			else if ( trimmed.Length == 0 )
			{
				FlushParagraph();
			}
			else if ( HeadingLevel( trimmed ) is > 0 and var level )
			{
				FlushParagraph();
				blocks.Add( new MarkdownBlock( MarkdownBlockKind.Heading ) { Level = level, Spans = ParseInline( trimmed.Substring( level ).Trim() ) } );
			}
			else if ( IsRule( trimmed ) )
			{
				FlushParagraph();
				blocks.Add( new MarkdownBlock( MarkdownBlockKind.Rule ) );
			}
			else if ( ListMarker( line, out _ ) is { } ordered )
			{
				FlushParagraph();
				index = ReadList( lines, index, ordered, blocks );
			}
			else if ( trimmed.StartsWith( '|' ) && index + 1 < lines.Length && IsTableSeparator( lines[ index + 1 ].Trim() ) )
			{
				FlushParagraph();
				index = ReadTable( lines, index, blocks );
			}
			else
			{
				paragraph.Add( trimmed );
			}
		}

		FlushParagraph();
		return blocks;
	}

	private static int ReadCode( string[] lines, int start, string language, List<MarkdownBlock> blocks )
	{
		var code = new StringBuilder();
		var index = start + 1;
		for ( ; index < lines.Length && !lines[ index ].Trim().StartsWith( "```", StringComparison.Ordinal ); index++ )
		{
			if ( code.Length > 0 ) code.Append( '\n' );
			code.Append( lines[ index ] );
		}
		blocks.Add( new MarkdownBlock( MarkdownBlockKind.Code ) { Code = code.ToString(), Language = language } );
		return index;
	}

	// Consecutive items of the same kind form one list; indented lines continue the previous item.
	private static int ReadList( string[] lines, int start, bool ordered, List<MarkdownBlock> blocks )
	{
		var items = new List<string>();
		var index = start;
		for ( ; index < lines.Length; index++ )
		{
			var line = lines[ index ];
			if ( ListMarker( line, out var text ) == ordered ) items.Add( text );
			else if ( items.Count > 0 && line.Length > 0 && char.IsWhiteSpace( line[ 0 ] ) && line.Trim().Length > 0 ) items[ ^1 ] += " " + line.Trim();
			else break;
		}
		blocks.Add( new MarkdownBlock( ordered ? MarkdownBlockKind.NumberedList : MarkdownBlockKind.BulletList )
		{
			Items = items.Select( ParseInline ).ToArray()
		} );
		return index - 1;
	}

	private static int ReadTable( string[] lines, int start, List<MarkdownBlock> blocks )
	{
		var header = Cells( lines[ start ] );
		var rows = new List<IReadOnlyList<IReadOnlyList<MarkdownSpan>>>();
		var index = start + 2;
		for ( ; index < lines.Length && lines[ index ].Trim().StartsWith( '|' ); index++ )
			rows.Add( Cells( lines[ index ] ).Select( ParseInline ).ToArray() );
		blocks.Add( new MarkdownBlock( MarkdownBlockKind.Table ) { Items = header.Select( ParseInline ).ToArray(), Rows = rows } );
		return index - 1;
	}

	private static string[] Cells( string line ) => line.Trim().Trim( '|' ).Split( '|' ).Select( cell => cell.Trim() ).ToArray();

	private static int HeadingLevel( string line )
	{
		var level = 0;
		while ( level < line.Length && level < 6 && line[ level ] == '#' ) level++;
		return level > 0 && level < line.Length && line[ level ] == ' ' ? level : 0;
	}

	private static bool IsRule( string line ) => line.Length >= 3 && (line.All( c => c == '-' ) || line.All( c => c == '*' ) || line.All( c => c == '_' ));

	private static bool IsTableSeparator( string line ) => line.StartsWith( '|' ) && line.Contains( '-' ) && line.All( c => c is '|' or '-' or ':' or ' ' );

	/// <summary>Returns true for a numbered item, false for a bullet, null when the line is not a list item.</summary>
	private static bool? ListMarker( string line, out string text )
	{
		text = "";
		var trimmed = line.TrimStart();
		if ( trimmed.Length > 2 && trimmed[ 0 ] is '-' or '*' or '+' && trimmed[ 1 ] == ' ' && !IsRule( trimmed.Trim() ) )
		{
			text = trimmed.Substring( 2 ).Trim();
			return false;
		}
		var digits = 0;
		while ( digits < trimmed.Length && char.IsAsciiDigit( trimmed[ digits ] ) ) digits++;
		if ( digits > 0 && digits + 1 < trimmed.Length && trimmed[ digits ] == '.' && trimmed[ digits + 1 ] == ' ' )
		{
			text = trimmed.Substring( digits + 2 ).Trim();
			return true;
		}
		return null;
	}

	/// <summary>Splits inline text into styled spans. Links keep their text and show the address after it.</summary>
	internal static IReadOnlyList<MarkdownSpan> ParseInline( string text )
	{
		var spans = new List<MarkdownSpan>();
		var plain = new StringBuilder();

		void Flush()
		{
			if ( plain.Length == 0 ) return;
			spans.Add( new MarkdownSpan( plain.ToString(), MarkdownSpanStyle.Plain ) );
			plain.Clear();
		}

		for ( var index = 0; index < text.Length; index++ )
		{
			var c = text[ index ];
			if ( c == '\\' && index + 1 < text.Length )
			{
				plain.Append( text[ ++index ] );
			}
			else if ( c == '`' && Closing( text, index + 1, "`" ) is var codeEnd && codeEnd > index )
			{
				Flush();
				spans.Add( new MarkdownSpan( text.Substring( index + 1, codeEnd - index - 1 ), MarkdownSpanStyle.Code ) );
				index = codeEnd;
			}
			else if ( c == '*' && index + 1 < text.Length && text[ index + 1 ] == '*' && Closing( text, index + 2, "**" ) is var boldEnd && boldEnd > index )
			{
				Flush();
				spans.Add( new MarkdownSpan( text.Substring( index + 2, boldEnd - index - 2 ), MarkdownSpanStyle.Bold ) );
				index = boldEnd + 1;
			}
			else if ( c is '*' or '_' && Closing( text, index + 1, c.ToString() ) is var italicEnd && italicEnd > index + 1 )
			{
				Flush();
				spans.Add( new MarkdownSpan( text.Substring( index + 1, italicEnd - index - 1 ), MarkdownSpanStyle.Italic ) );
				index = italicEnd;
			}
			else if ( c == '[' && TryLink( text, index, out var label, out var address, out var linkEnd ) )
			{
				plain.Append( label ).Append( " (" ).Append( address ).Append( ')' );
				index = linkEnd;
			}
			else
			{
				plain.Append( c );
			}
		}

		Flush();
		return spans;
	}

	private static int Closing( string text, int from, string marker ) => from < text.Length ? text.IndexOf( marker, from, StringComparison.Ordinal ) : -1;

	private static bool TryLink( string text, int start, out string label, out string address, out int end )
	{
		label = address = "";
		end = start;
		var close = text.IndexOf( ']', start + 1 );
		if ( close < 0 || close + 1 >= text.Length || text[ close + 1 ] != '(' ) return false;
		var paren = text.IndexOf( ')', close + 2 );
		if ( paren < 0 ) return false;
		label = text.Substring( start + 1, close - start - 1 );
		address = text.Substring( close + 2, paren - close - 2 );
		end = paren;
		return true;
	}
}
