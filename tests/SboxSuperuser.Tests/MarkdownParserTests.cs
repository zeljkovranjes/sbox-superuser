using SboxSuperuser.Core.Documentation;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class MarkdownParserTests
{
	[Fact]
	public void BlocksAreRecognizedInOrder()
	{
		var blocks = MarkdownParser.Parse( """
			# Title

			First line
			continues here.

			- one
			- two
			  more

			1. first
			2. second

			```effect
			color = red
			```

			| Key | Use |
			|---|---|
			| `a` | b |

			---
			""" );

		Assert.Equal( new[]
		{
			MarkdownBlockKind.Heading, MarkdownBlockKind.Paragraph, MarkdownBlockKind.BulletList,
			MarkdownBlockKind.NumberedList, MarkdownBlockKind.Code, MarkdownBlockKind.Table, MarkdownBlockKind.Rule
		}, blocks.Select( block => block.Kind ) );
		Assert.Equal( 1, blocks[ 0 ].Level );
		Assert.Equal( "First line continues here.", blocks[ 1 ].Spans.Single().Text );
		Assert.Equal( "two more", blocks[ 2 ].Items[ 1 ].Single().Text );
		Assert.Equal( "effect", blocks[ 4 ].Language );
		Assert.Equal( "color = red", blocks[ 4 ].Code );
		Assert.Equal( "Key", blocks[ 5 ].Items[ 0 ].Single().Text );
		Assert.Equal( MarkdownSpanStyle.Code, blocks[ 5 ].Rows[ 0 ][ 0 ].Single().Style );
	}

	[Fact]
	public void InlineStylesAndLinksAreSplitIntoSpans()
	{
		var spans = MarkdownParser.ParseInline( "Use **bold**, *soft*, `code` and [docs](https://x.y) \\*here\\*" );
		Assert.Contains( spans, span => span.Style == MarkdownSpanStyle.Bold && span.Text == "bold" );
		Assert.Contains( spans, span => span.Style == MarkdownSpanStyle.Italic && span.Text == "soft" );
		Assert.Contains( spans, span => span.Style == MarkdownSpanStyle.Code && span.Text == "code" );
		Assert.Contains( spans, span => span.Text.Contains( "docs (https://x.y) *here*" ) );
	}

	[Fact]
	public void UnclosedMarkersAndOddInputStayPlainText()
	{
		Assert.Equal( "a * b ` c", MarkdownParser.ParseInline( "a * b ` c" ).Single().Text );
		Assert.Single( MarkdownParser.Parse( "```\nunclosed code" ) );
		Assert.Empty( MarkdownParser.Parse( "" ) );
	}

	[Fact]
	public void PagesEnforceTheirLimits()
	{
		Assert.Throws<ArgumentException>( () => DocumentationPage.Create( "", "text" ) );
		Assert.Throws<ArgumentException>( () => DocumentationPage.Create( "Title", " " ) );
		Assert.Throws<ArgumentException>( () => DocumentationPage.Create( new string( 'x', 65 ), "text" ) );
		Assert.Throws<ArgumentException>( () => DocumentationPage.Create( "Title", new string( 'x', DocumentationPage.MaximumMarkdownLength + 1 ) ) );
		Assert.Equal( "a\nb", DocumentationPage.Create( " Title ", "a\r\nb" ).Markdown );
	}
}
