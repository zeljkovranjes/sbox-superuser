using System.Text;

namespace SboxSuperuser.Core.Storage;

internal sealed partial class StoreCoordinator
{
	internal async Task<StorageOutcome> WriteAsync( string directory, int schema, long expectedRevision,
		string payload, CancellationToken cancellation )
	{
		return await SerializedAsync( async () =>
		{
			if ( schema != 1 ) return new StorageOutcome( StorageStatus.UnsupportedSchema );
			try { TransactionFormat.Validate( payload ); }
			catch ( Exception error ) when ( IsDataError( error ) ) { return new StorageOutcome( StorageStatus.InvalidData ); }
			var current = ReadCurrent( directory, schema );
			if ( current.Status is not (StorageStatus.Found or StorageStatus.Missing) ) return current;
			if ( expectedRevision < 0 || expectedRevision == long.MaxValue
				|| (current.Document?.Revision ?? 0) != expectedRevision ) return new StorageOutcome( StorageStatus.Conflict );
			return await CommitAsync( directory, new StoredRevision( Guid.NewGuid(), schema, expectedRevision + 1, payload ), cancellation );
		}, cancellation );
	}

	private async Task<StorageOutcome> CommitAsync( string directory, StoredRevision document, CancellationToken cancellation )
	{
		var commitAttempted = false;
		try
		{
			var generation = StoreFormat.Generation( document );
			if ( Encoding.UTF8.GetByteCount( generation ) > StoreFormat.MaximumBytes ) return new StorageOutcome( StorageStatus.InvalidData );
			var marker = StoreFormat.Commit( document, generation );
			var stem = StoreFormat.Stem( document );
			var generationPath = $"{directory}/g-{stem}.json";
			var markerPath = $"{directory}/c-{stem}.json";
			cancellation.ThrowIfCancellationRequested();
			if ( !_available() ) return new StorageOutcome( StorageStatus.Unavailable );
			_files.CreateDirectory( directory );
			await _files.WriteNewAsync( generationPath, generation, cancellation );
			if ( _files.Read( generationPath ) != generation ) return new StorageOutcome( StorageStatus.Unavailable );
			cancellation.ThrowIfCancellationRequested();
			if ( !_available() ) return new StorageOutcome( StorageStatus.Unavailable );
			commitAttempted = true;
			await _files.WriteNewAsync( markerPath, marker, cancellation );
			if ( _files.Read( markerPath ) != marker || _files.Read( generationPath ) != generation )
			{
				return new StorageOutcome( StorageStatus.Unknown );
			}

			var verified = Decode( directory, $"c-{stem}.json" );
			return new StorageOutcome( StorageStatus.Committed, verified );
		}
		catch ( OperationCanceledException )
		{
			return new StorageOutcome( commitAttempted ? StorageStatus.Unknown : StorageStatus.Cancelled );
		}
		catch ( Exception error ) when ( error is IOException or UnauthorizedAccessException || IsDataError( error ) )
		{
			return new StorageOutcome( commitAttempted ? StorageStatus.Unknown : StorageStatus.Unavailable );
		}
	}
}
