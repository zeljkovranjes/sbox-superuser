using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SboxSuperuser.Core.Storage;

internal static class StoreFormat
{
	internal const int MaximumBytes = 1048576;

	internal static string Hash( string value ) => Convert.ToHexString( SHA256.HashData( Encoding.UTF8.GetBytes( value ) ) );

	internal static string Stem( StoredRevision document ) => $"{document.Revision:D20}-{document.GenerationId:N}";

	internal static long RevisionFromName( string name )
	{
		if ( name.Length != 60 || name[ 22 ] != '-' || !name.EndsWith( ".json", StringComparison.Ordinal )
			|| !long.TryParse( name.Substring( 2, 20 ), NumberStyles.None, CultureInfo.InvariantCulture, out var revision )
			|| revision < 1 || !Guid.TryParseExact( name.Substring( 23, 32 ), "N", out _ ) )
		{
			throw new FormatException( "Unrecognized storage record name." );
		}

		return revision;
	}

	internal static string Generation( StoredRevision value ) => JsonSerializer.Serialize( new
	{
		formatVersion = 1,
		generation = value.GenerationId.ToString( "N" ),
		schemaVersion = value.SchemaVersion,
		revision = value.Revision,
		payload = value.PayloadJson
	} );

	internal static string Commit( StoredRevision value, string generationText ) => JsonSerializer.Serialize( new
	{
		formatVersion = 1,
		generation = value.GenerationId.ToString( "N" ),
		revision = value.Revision,
		digest = Hash( generationText )
	} );

	internal static StoredRevision Decode( string commitName, string commitText, string generationText )
	{
		using var commit = JsonDocument.Parse( commitText );
		using var generation = JsonDocument.Parse( generationText );
		var marker = commit.RootElement;
		var data = generation.RootElement;
		RequireFields( marker, "formatVersion", "generation", "revision", "digest" );
		RequireFields( data, "formatVersion", "generation", "schemaVersion", "revision", "payload" );
		if ( marker.GetProperty( "formatVersion" ).GetInt32() != 1 || data.GetProperty( "formatVersion" ).GetInt32() != 1 )
		{
			throw new UnsupportedStoreSchemaException();
		}

		var revision = marker.GetProperty( "revision" ).GetInt64();
		var id = Guid.ParseExact( marker.GetProperty( "generation" ).GetString(), "N" );
		if ( id == Guid.Empty || revision != RevisionFromName( commitName )
			|| commitName != $"c-{revision:D20}-{id:N}.json"
			|| data.GetProperty( "revision" ).GetInt64() != revision
			|| data.GetProperty( "generation" ).GetString() != id.ToString( "N" )
			|| marker.GetProperty( "digest" ).GetString() != Hash( generationText ) )
		{
			throw new FormatException( "Storage record integrity check failed." );
		}

		var schema = data.GetProperty( "schemaVersion" ).GetInt32();
		if ( schema != 1 ) throw new UnsupportedStoreSchemaException();
		var payload = data.GetProperty( "payload" ).GetString();
		TransactionFormat.Validate( payload );
		return new StoredRevision( id, schema, revision, payload );
	}

	internal static void RequireFields( JsonElement value, params string[] fields )
	{
		if ( value.ValueKind != JsonValueKind.Object ) throw new FormatException( "Expected an object." );
		var names = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var property in value.EnumerateObject() )
		{
			if ( !names.Add( property.Name ) || !fields.Contains( property.Name ) )
			{
				throw new FormatException( "Unknown or duplicate record field." );
			}
		}

		if ( names.Count != fields.Length ) throw new FormatException( "Missing record field." );
	}
}
