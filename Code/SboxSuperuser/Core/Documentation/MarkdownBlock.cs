namespace SboxSuperuser.Core.Documentation;

/// <summary>
/// One block of a parsed page. Which fields are set depends on <see cref="Kind"/>: headings and paragraphs use
/// <see cref="Spans"/>, lists use <see cref="Items"/>, code uses <see cref="Code"/> and <see cref="Language"/>,
/// tables use <see cref="Items"/> for the header cells and <see cref="Rows"/> for the body.
/// </summary>
internal sealed record MarkdownBlock( MarkdownBlockKind Kind )
{
	internal int Level { get; init; }
	internal IReadOnlyList<MarkdownSpan> Spans { get; init; } = Array.Empty<MarkdownSpan>();
	internal IReadOnlyList<IReadOnlyList<MarkdownSpan>> Items { get; init; } = Array.Empty<IReadOnlyList<MarkdownSpan>>();
	internal IReadOnlyList<IReadOnlyList<IReadOnlyList<MarkdownSpan>>> Rows { get; init; } = Array.Empty<IReadOnlyList<IReadOnlyList<MarkdownSpan>>>();
	internal string Code { get; init; } = "";
	internal string Language { get; init; } = "";
}
