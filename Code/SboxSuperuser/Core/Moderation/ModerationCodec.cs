using System.Globalization;
using System.Text;
using System.Text.Json;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Moderation;

internal static class ModerationCodec
{
	internal static ModerationLedger Read( string payload, long revision )
	{
		using var json = JsonDocument.Parse( payload );
		var state = json.RootElement.GetProperty( "state" );
		StoreFormat.RequireFields( state, "schema", "entries" );
		if ( state.GetProperty( "schema" ).GetInt32() != 1 ) throw new UnsupportedStoreSchemaException();
		var records = state.GetProperty( "entries" );
		if ( records.ValueKind != JsonValueKind.Array || records.GetArrayLength() > ModerationLedger.MaximumRecords )
			throw new FormatException( "Moderation history capacity exceeded." );
		return new ModerationLedger( revision, records.EnumerateArray().Select( ReadEntry ).ToArray() );
	}

	private static ModerationEntry ReadEntry( JsonElement entry )
	{
		StoreFormat.RequireFields( entry, "id", "requestId", "target", "kind", "actorKind", "actorSubject", "origin", "reason", "createdUtc", "expiresUtc", "revokes" );
		var kindName = entry.GetProperty( "kind" ).GetString();
		if ( !Enum.TryParse<ModerationKind>( kindName, out var kind ) || kind.ToString() != kindName )
			throw new FormatException( "Unknown moderation kind." );
		return new ModerationEntry( entry.GetProperty( "id" ).GetGuid(), entry.GetProperty( "requestId" ).GetGuid(),
			entry.GetProperty( "target" ).GetString(), kind, entry.GetProperty( "actorKind" ).GetString(),
			entry.GetProperty( "actorSubject" ).GetString(), entry.GetProperty( "origin" ).GetString(), entry.GetProperty( "reason" ).GetString(),
			Timestamp( entry.GetProperty( "createdUtc" ) ),
			entry.GetProperty( "expiresUtc" ).ValueKind == JsonValueKind.Null ? null : Timestamp( entry.GetProperty( "expiresUtc" ) ),
			entry.GetProperty( "revokes" ).ValueKind == JsonValueKind.Null ? null : entry.GetProperty( "revokes" ).GetGuid() );
	}

	private static DateTimeOffset Timestamp( JsonElement value ) => DateTimeOffset.ParseExact( value.GetString(), "O", CultureInfo.InvariantCulture, DateTimeStyles.None );

	internal static string WriteState( ModerationLedger ledger )
	{
		var state = JsonSerializer.Serialize( new
		{
			schema = 1,
			entries = ledger.Entries.Select( entry => new
			{
				id = entry.Id, requestId = entry.RequestId, target = entry.Target, kind = entry.Kind.ToString(),
				actorKind = entry.ActorKind, actorSubject = entry.ActorSubject, origin = entry.Origin, reason = entry.Reason,
				createdUtc = entry.CreatedUtc.ToString( "O", CultureInfo.InvariantCulture ),
				expiresUtc = entry.ExpiresUtc?.ToString( "O", CultureInfo.InvariantCulture ), revokes = entry.Revokes
			} ).ToArray()
		} );
		if ( Encoding.UTF8.GetByteCount( state ) > 384000 ) throw new InvalidOperationException( "Encoded moderation history capacity exceeded." );
		return state;
	}
}
