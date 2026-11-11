using System.Text.Json;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Content;

/// <summary>Stores bans as {"schema":1,"bans":[{"kind","pattern","reason"}]}.</summary>
internal static class ContentBanCodec
{
	internal static IReadOnlyList<ContentBan> ReadState( string stateJson )
	{
		using var json = JsonDocument.Parse( stateJson );
		var state = json.RootElement;
		StoreFormat.RequireFields( state, "schema", "bans" );
		if ( state.GetProperty( "schema" ).GetInt32() != 1 ) throw new UnsupportedStoreSchemaException();
		return state.GetProperty( "bans" ).EnumerateArray().Select( ban => new ContentBan(
			ban.GetProperty( "kind" ).GetString(), ban.GetProperty( "pattern" ).GetString(), ban.GetProperty( "reason" ).GetString() ) ).ToArray();
	}

	internal static string WriteState( IEnumerable<ContentBan> bans ) => JsonSerializer.Serialize( new
	{
		schema = 1,
		bans = bans.Select( ban => new { kind = ban.Kind, pattern = ban.Pattern, reason = ban.Reason } ).ToArray()
	} );
}
