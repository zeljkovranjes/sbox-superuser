using Sandbox;
using SboxSuperuser.Core.Content;

namespace SboxSuperuser.Engine.Content;

/// <summary>
/// Finds maps and addons among s&amp;box packages and entities among the prefabs and models this server has loaded;
/// package thumbnails are URLs and entity icons use the engine's thumb: images.
/// </summary>
internal sealed class EngineContentCatalog : IContentCatalog
{
	private const string MapPackageType = "map";
	private const string GamePackageType = "game";

	// Package lookups go to the backend, so each banned ident is described once per session.
	private readonly Dictionary<string, ContentCatalogItem> _described = new( StringComparer.OrdinalIgnoreCase );

	public async Task<IReadOnlyList<ContentCatalogItem>> SearchAsync( string kind, string query, int take )
	{
		query = query?.Trim() ?? "";
		if ( kind == ContentBanList.Entity ) return SearchResources( query, take );
		var search = kind == ContentBanList.Map ? $"type:map {query}".Trim() : query.Length > 0 ? query : "type:addon sort:popular";
		try
		{
			var found = await Package.FindAsync( search, take * 2 );
			return found.Packages
				.Where( package => kind == ContentBanList.Map ? package.TypeName == MapPackageType : package.TypeName is not (MapPackageType or GamePackageType) )
				.Take( take ).Select( Item ).ToArray();
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Package search failed; query={search}; type={error.GetType().Name}: {error.Message}" );
			return Array.Empty<ContentCatalogItem>();
		}
	}

	public async Task<ContentCatalogItem> DescribeAsync( string kind, string id )
	{
		if ( id.Contains( '*' ) ) return null;
		if ( kind == ContentBanList.Entity ) return new ContentCatalogItem( id, System.IO.Path.GetFileNameWithoutExtension( id ), $"thumb:{id}" );
		if ( _described.TryGetValue( id, out var known ) ) return known;
		try
		{
			var package = await Package.FetchAsync( id, true );
			return _described[ id ] = package is null ? null : Item( package );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Package lookup failed; ident={id}; type={error.GetType().Name}: {error.Message}" );
			return null;
		}
	}

	private static ContentCatalogItem Item( Package package ) => new( package.FullIdent, package.Title, package.Thumb ?? "" );

	private static IReadOnlyList<ContentCatalogItem> SearchResources( string query, int take ) =>
		ResourceLibrary.GetAll<PrefabFile>().Select( prefab => prefab.ResourcePath )
			.Concat( ResourceLibrary.GetAll<Model>().Where( model => !model.IsError ).Select( model => model.ResourcePath ) )
			.Where( path => !string.IsNullOrEmpty( path ) && path.Contains( query, StringComparison.OrdinalIgnoreCase ) )
			.Distinct( StringComparer.OrdinalIgnoreCase ).OrderBy( path => path, StringComparer.OrdinalIgnoreCase ).Take( take )
			.Select( path => new ContentCatalogItem( path, System.IO.Path.GetFileNameWithoutExtension( path ), $"thumb:{path}" ) ).ToArray();
}
