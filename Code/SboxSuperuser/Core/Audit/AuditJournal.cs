using System.Text;

namespace SboxSuperuser.Core.Audit;

internal sealed class AuditJournal
{
	internal const int MaximumStateBytes = 384000;
	internal int MaximumRecords { get; }
	internal int RetentionDays { get; }
	internal long NextSequence { get; }
	internal IReadOnlyList<AuditRecord> Records { get; }
	internal bool NeedsRetentionCheckpoint { get; }

	internal AuditJournal( int maximumRecords = 512, int retentionDays = 30, long nextSequence = 1, IEnumerable<AuditRecord> records = null,
		bool needsRetentionCheckpoint = false )
	{
		if ( maximumRecords is < 1 or > 4096 || retentionDays is < 1 or > 3650 || nextSequence < 1 )
		{
			throw new ArgumentException( "Audit retention must be 1–4096 records and 1–3650 days, with a positive sequence." );
		}

		var copy = (records ?? Array.Empty<AuditRecord>()).Take( 4098 ).ToArray();
		if ( copy.Length > 4097 || copy.Any( record => record is null || record.Sequence >= nextSequence )
			|| copy.Select( record => record.Id ).Distinct().Count() != copy.Length
			|| copy.Select( record => record.Sequence ).Distinct().Count() != copy.Length )
		{
			throw new ArgumentException( "Audit records have duplicate IDs/sequences or exceed their limits." );
		}

		MaximumRecords = maximumRecords;
		RetentionDays = retentionDays;
		NextSequence = nextSequence;
		Records = Array.AsReadOnly( copy.OrderBy( record => record.Sequence ).ToArray() );
		NeedsRetentionCheckpoint = needsRetentionCheckpoint;
	}

	internal AuditJournal Append( AuditRecord record )
	{
		if ( record.Sequence != NextSequence || NextSequence == long.MaxValue )
		{
			throw new InvalidOperationException( "Audit append requires the next sequence." );
		}

		return new AuditJournal( MaximumRecords, RetentionDays, NextSequence + 1, Records.Append( record ), NeedsRetentionCheckpoint );
	}

	internal AuditJournal Complete( Guid id, string outcome, DateTimeOffset utc )
	{
		if ( !Records.Any( record => record.Id == id ) )
		{
			throw new InvalidOperationException( "The action intent is not retained." );
		}

		return new AuditJournal( MaximumRecords, RetentionDays, NextSequence,
			Records.Select( record => record.Id == id ? record.Complete( outcome, utc ) : record ), NeedsRetentionCheckpoint );
	}

	internal AuditJournal Trim( DateTimeOffset now, IReadOnlySet<Guid> protectedIds )
	{
		var selected = Records.Where( record => protectedIds.Contains( record.Id ) || !IsExpired( record, now ) ).ToList();
		var sizes = selected.ToDictionary( record => record.Id, record => Encoding.UTF8.GetByteCount( AuditFormat.WriteRecord( record ) ) + 1 );
		var size = sizes.Values.Sum() + 256;
		while ( selected.Count > MaximumRecords || size > MaximumStateBytes )
		{
			var index = selected.FindIndex( record => !protectedIds.Contains( record.Id ) );
			if ( index < 0 )
			{
				throw new InvalidOperationException( "Active audit intents exceed the retention capacity." );
			}

			size -= sizes[ selected[ index ].Id ];
			selected.RemoveAt( index );
		}

		return new AuditJournal( MaximumRecords, RetentionDays, NextSequence, selected, NeedsRetentionCheckpoint );
	}

	internal AuditJournal WithRetentionCheckpoint( bool required ) => new( MaximumRecords, RetentionDays, NextSequence, Records, required );

	internal bool IsExpired( AuditRecord record, DateTimeOffset now ) => now - record.CreatedUtc >= TimeSpan.FromDays( RetentionDays );
}
