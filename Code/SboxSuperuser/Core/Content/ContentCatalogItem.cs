namespace SboxSuperuser.Core.Content;

/// <summary>Something that can be banned: the ID a ban matches, a title to show, and an icon path or URL (empty for none).</summary>
internal sealed record ContentCatalogItem( string Id, string Title, string Icon );
