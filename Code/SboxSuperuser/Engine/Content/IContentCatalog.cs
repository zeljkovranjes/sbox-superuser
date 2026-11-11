using SboxSuperuser.Core.Content;

namespace SboxSuperuser.Engine.Content;

/// <summary>The maps, addons and entities that could be banned, as the host can find them.</summary>
internal interface IContentCatalog
{
	/// <summary>Up to take items of this kind matching the query; an empty query lists popular or all. Empty when the lookup fails.</summary>
	Task<IReadOnlyList<ContentCatalogItem>> SearchAsync( string kind, string query, int take );

	/// <summary>The title and icon for one exact ID, or null when it can't be found or the ID is a pattern.</summary>
	Task<ContentCatalogItem> DescribeAsync( string kind, string id );
}
