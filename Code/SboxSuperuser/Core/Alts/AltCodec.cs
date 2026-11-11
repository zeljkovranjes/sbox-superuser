using System.Globalization;
using System.Text;
using System.Text.Json;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Alts;

/// <summary>
/// Stores the ledger with short keys, dropping the longest-unseen accounts if it would outgrow one stored document.
/// Schema 1 kept salted address hashes; it is ignored and the ledger starts fresh.
/// </summary>
internal static class AltCodec
{
	private const int ByteBudget = StoreFormat.MaximumBytes / 2 - 16384;

	internal const int Schema = 2;

	internal static IReadOnlyList<AltIdentity> ReadState( string stateJson )
	{
		using var json = JsonDocument.Parse( stateJson );
		var state = json.RootElement;
		StoreFormat.RequireFields( state, "schema", "ids" );
		var schema = state.GetProperty( "schema" ).GetInt32();
		if ( schema == 1 ) return Array.Empty<AltIdentity>();
		if ( schema != Schema ) throw new UnsupportedStoreSchemaException();
		var identities = state.GetProperty( "ids" ).EnumerateArray().Select( entry => new AltIdentity
		{
			SteamId = entry.GetProperty( "s" ).GetString(),
			FirstSeenUtc = DateTimeOffset.Parse( entry.GetProperty( "f" ).GetString(), CultureInfo.InvariantCulture ),
			LastSeenUtc = DateTimeOffset.Parse( entry.GetProperty( "l" ).GetString(), CultureInfo.InvariantCulture ),
			Names = Strings( entry, "n" ), Owners = Strings( entry, "o" ), Parties = Strings( entry, "p" ),
			Addresses = entry.GetProperty( "a" ).EnumerateArray().Select( address => new AltAddress( address.GetProperty( "ip" ).GetString(),
				Time( address.GetProperty( "f" ) ), Time( address.GetProperty( "l" ) ) ) ).ToList()
		} ).ToArray();
		return identities;
	}

	private static DateTimeOffset Time( JsonElement value ) => DateTimeOffset.Parse( value.GetString(), CultureInfo.InvariantCulture );

	internal static string WriteState( IEnumerable<AltIdentity> identities )
	{
		var kept = identities.OrderByDescending( identity => identity.LastSeenUtc ).ToList();
		while ( true )
		{
			var state = JsonSerializer.Serialize( new
			{
				schema = Schema,
				ids = kept.Select( identity => new
				{
					s = identity.SteamId, f = identity.FirstSeenUtc.ToString( "O", CultureInfo.InvariantCulture ),
					l = identity.LastSeenUtc.ToString( "O", CultureInfo.InvariantCulture ),
					n = identity.Names, o = identity.Owners, p = identity.Parties,
					a = identity.Addresses.Select( address => new { ip = address.Address, f = address.FirstSeenUtc.ToString( "O", CultureInfo.InvariantCulture ),
						l = address.LastSeenUtc.ToString( "O", CultureInfo.InvariantCulture ) } )
				} )
			} );
			if ( Encoding.UTF8.GetByteCount( state ) <= ByteBudget || kept.Count == 0 ) return state;
			kept.RemoveRange( kept.Count * 9 / 10, kept.Count - kept.Count * 9 / 10 );
		}
	}

	private static List<string> Strings( JsonElement entry, string name ) =>
		entry.GetProperty( name ).EnumerateArray().Select( value => value.GetString() ).Where( value => !string.IsNullOrEmpty( value ) ).ToList();
}
