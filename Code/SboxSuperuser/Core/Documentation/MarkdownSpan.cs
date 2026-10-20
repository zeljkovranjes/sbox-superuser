namespace SboxSuperuser.Core.Documentation;

/// <summary>A run of inline text with one style.</summary>
internal sealed record MarkdownSpan( string Text, MarkdownSpanStyle Style );
