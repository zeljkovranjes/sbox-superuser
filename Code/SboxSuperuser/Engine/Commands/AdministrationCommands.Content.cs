using SboxSuperuser.Core.Content;
using SboxSuperuser.Engine.Content;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	internal const string ContentPermission = "admin.content.manage";
	// The dispatcher caps a result at 16 fields and 8 KB, so a search returns what fits.
	private const int CatalogResults = 16;
	private const int CatalogBytes = 7680;
	private const int MaximumSearchLength = 64;

	private void RegisterContent( HostContentService content, IContentCatalog catalog )
	{
		var kinds = string.Join( ", ", ContentBanList.Kinds );
		Register( "content.ban", $"Ban a map, addon or entity ({kinds}); * matches anything, as in author.*", ContentPermission,
			new[] { Kind(), Pattern(), new CommandArgumentDescriptor( "reason", "Why, shown when it is refused", CommandArgumentKind.String, isOptional: true, isRemainder: true ) },
			(context, arguments) => content.BanAsync( context, arguments.Get<string>( "kind" ), arguments.Get<string>( "pattern" ),
				arguments.Contains( "reason" ) ? arguments.Get<string>( "reason" ) : "" ), mutation: true );
		Register( "content.unban", "Allow a banned map, addon or entity again", ContentPermission, new[] { Kind(), Pattern() },
			(context, arguments) => content.UnbanAsync( context, arguments.Get<string>( "kind" ), arguments.Get<string>( "pattern" ) ), mutation: true );
		Register( "content.list", "List banned maps, addons and entities", ContentPermission, new[] { Page() },
			(_, arguments) => Task.FromResult( Rows( content.Bans.Bans.Select( ban => $"{ban.Kind} {ban.Pattern}{(ban.Reason.Length > 0 ? " — " + ban.Reason : "")}" ),
				PageNumber( arguments ), content.Bans.Bans.Count == 0 ? "Nothing is banned." : "Banned content." ) ) );
		if ( catalog is null ) return;
		Register( "content.search", "Find maps, addons or entities that aren't banned yet, with their icons", ContentPermission,
			new[] { Kind(), new CommandArgumentDescriptor( "query", "Words to search for; empty lists popular or all", CommandArgumentKind.String, isOptional: true,
				isRemainder: true, audit: CommandArgumentAudit.Redacted ) },
			(_, arguments) => SearchContentAsync( content, catalog, arguments.Get<string>( "kind" )?.ToLowerInvariant(),
				arguments.Contains( "query" ) ? arguments.Get<string>( "query" ) : "" ) );
	}

	// Results go back as item1, item2... JSON fields, the shape the menu's catalog lists read; banned items are left out.
	private static async Task<CommandResult> SearchContentAsync( HostContentService content, IContentCatalog catalog, string kind, string query )
	{
		if ( !ContentBanList.Kinds.Contains( kind ) ) return new CommandResult( CommandStatus.InvalidArguments, $"Content is one of {string.Join( ", ", ContentBanList.Kinds )}." );
		if ( query.Length > MaximumSearchLength ) return new CommandResult( CommandStatus.InvalidArguments, $"Searches are at most {MaximumSearchLength} characters." );
		var found = (await catalog.SearchAsync( kind, query, CatalogResults )).Where( item => content.IsAllowed( kind, item.Id ) ).ToArray();
		var fields = new Dictionary<string, string>( StringComparer.Ordinal );
		var size = 0;
		foreach ( var item in found )
		{
			if ( fields.Count == CatalogResults ) break;
			var json = System.Text.Json.JsonSerializer.Serialize( new AdminCatalogItem { Id = item.Id, Title = Printable( item.Title ), Icon = item.Icon } );
			if ( json.Length > 1024 || size + json.Length > CatalogBytes ) continue;
			size += json.Length;
			fields[ "item" + (fields.Count + 1) ] = json;
		}
		return new CommandResult( CommandStatus.Success, fields.Count == 0 ? "Nothing found." : $"{fields.Count} found.", fields );
	}

	private static string Printable( string text ) => new( (text ?? "").Where( character => !char.IsControl( character ) ).ToArray() );

	private static CommandArgumentDescriptor Kind() => new( "kind", "map, addon or entity", CommandArgumentKind.String, audit: CommandArgumentAudit.Redacted );

	private static CommandArgumentDescriptor Pattern() => new( "pattern", "Map or package ident, prefab or model path, or component type; * matches anything",
		CommandArgumentKind.String );
}
