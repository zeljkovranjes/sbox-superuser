namespace SboxSuperuser.Core.Documentation;

/// <summary>One page a module wrote about itself, in Markdown.</summary>
internal sealed record DocumentationPage( string Title, string Markdown )
{
	internal const int MaximumPages = 32;
	internal const int MaximumTitleLength = 64;
	internal const int MaximumMarkdownLength = 32 * 1024;

	/// <summary>Checks a page before it is staged; the message says which limit was broken.</summary>
	internal static DocumentationPage Create( string title, string markdown )
	{
		title = title?.Trim() ?? "";
		if ( title.Length == 0 || title.Length > MaximumTitleLength || title.Any( char.IsControl ) )
			throw new ArgumentException( $"Documentation titles need 1 to {MaximumTitleLength} printable characters.", nameof( title ) );
		if ( string.IsNullOrWhiteSpace( markdown ) || markdown.Length > MaximumMarkdownLength )
			throw new ArgumentException( $"Documentation pages need some text and at most {MaximumMarkdownLength} characters.", nameof( markdown ) );
		return new DocumentationPage( title, markdown.Replace( "\r\n", "\n" ) );
	}
}
