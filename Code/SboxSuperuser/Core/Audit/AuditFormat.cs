using System.Globalization;
using System.Text.Json;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Audit;

internal static class AuditFormat
{
	internal static string WriteRecord( AuditRecord record ) => JsonSerializer.Serialize( new
	{
		sequence = record.Sequence, id = record.Id, requestId = record.RequestId, actorKind = record.ActorKind,
		actorSubject = record.ActorSubject, origin = record.Origin, command = record.Command,
		safeArguments = record.SafeArguments, targets = record.Targets, createdUtc = record.CreatedUtc.ToString( "O", CultureInfo.InvariantCulture ),
		completedUtc = record.CompletedUtc?.ToString( "O", CultureInfo.InvariantCulture ), outcome = record.Outcome
	} );

	internal static string WriteState( AuditJournal journal ) => "{\"auditSchema\":1,\"maximumRecords\":"
		+ journal.MaximumRecords.ToString( CultureInfo.InvariantCulture ) + ",\"retentionDays\":" + journal.RetentionDays.ToString( CultureInfo.InvariantCulture )
		+ ",\"nextSequence\":" + journal.NextSequence.ToString( CultureInfo.InvariantCulture )
		+ ",\"needsRetentionCheckpoint\":" + (journal.NeedsRetentionCheckpoint ? "true" : "false")
		+ ",\"records\":[" + string.Join( ",", journal.Records.Select( WriteRecord ) ) + "]}";

	internal static AuditJournal ReadPayload( string payload )
	{
		using var document = JsonDocument.Parse( payload, new JsonDocumentOptions { MaxDepth = 32 } );
		var state = document.RootElement.GetProperty( "state" );
		StoreFormat.RequireFields( state, "auditSchema", "maximumRecords", "retentionDays", "nextSequence", "needsRetentionCheckpoint", "records" );
		if ( state.GetProperty( "auditSchema" ).GetInt32() != 1 )
		{
			throw new UnsupportedStoreSchemaException();
		}

		var records = state.GetProperty( "records" );
		if ( records.ValueKind != JsonValueKind.Array || records.GetArrayLength() > 4096 )
		{
			throw new FormatException( "Invalid audit record array." );
		}

		var journal = new AuditJournal( state.GetProperty( "maximumRecords" ).GetInt32(), state.GetProperty( "retentionDays" ).GetInt32(),
			state.GetProperty( "nextSequence" ).GetInt64(), records.EnumerateArray().Select( ReadRecord ), state.GetProperty( "needsRetentionCheckpoint" ).GetBoolean() );
		if ( journal.Records.Count > journal.MaximumRecords || System.Text.Encoding.UTF8.GetByteCount( WriteState( journal ) ) > AuditJournal.MaximumStateBytes )
		{
			throw new FormatException( "Stored audit data exceeds retention bounds." );
		}

		return journal;
	}

	private static AuditRecord ReadRecord( JsonElement record )
	{
		StoreFormat.RequireFields( record, "sequence", "id", "requestId", "actorKind", "actorSubject", "origin", "command",
			"safeArguments", "targets", "createdUtc", "completedUtc", "outcome" );
		var arguments = record.GetProperty( "safeArguments" ).EnumerateObject().ToDictionary( pair => pair.Name, pair => pair.Value.GetString(), StringComparer.Ordinal );
		var completed = record.GetProperty( "completedUtc" );
		return new AuditRecord( record.GetProperty( "sequence" ).GetInt64(), record.GetProperty( "id" ).GetGuid(), record.GetProperty( "requestId" ).GetGuid(),
			record.GetProperty( "actorKind" ).GetString(), record.GetProperty( "actorSubject" ).GetString(), record.GetProperty( "origin" ).GetString(),
			record.GetProperty( "command" ).GetString(), arguments, record.GetProperty( "targets" ).EnumerateArray().Select( value => value.GetString() ),
			Timestamp( record.GetProperty( "createdUtc" ).GetString() ), record.GetProperty( "outcome" ).GetString(),
			completed.ValueKind == JsonValueKind.Null ? null : Timestamp( completed.GetString() ) );
	}

	private static DateTimeOffset Timestamp( string text )
	{
		if ( !DateTimeOffset.TryParseExact( text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value ) || value.Offset != TimeSpan.Zero )
		{
			throw new FormatException( "Audit timestamps must be canonical UTC values." );
		}

		return value;
	}
}
