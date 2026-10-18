using SboxSuperuser.Core.Audit;

namespace SboxSuperuser.Core.Storage;

internal sealed partial class StoreCoordinator
{
	internal async Task<StoragePruneResult> PruneAuditHistoryAsync( string directory, long expectedRevision, CancellationToken cancellation )
	{
		var removed = 0;
		var pending = false;
		var result = await SerializedAsync( () =>
		{
			if ( _files is not IStoreRetentionFiles retention )
			{
				return Task.FromResult( new StorageOutcome( StorageStatus.Unavailable ) );
			}

			var current = ReadCurrent( directory, 1 );
			if ( current.Status == StorageStatus.Missing && expectedRevision == 0 )
			{
				return Task.FromResult( new StorageOutcome( StorageStatus.Committed ) );
			}

			if ( current.Status != StorageStatus.Found )
			{
				return Task.FromResult( current );
			}

			if ( current.Document.Revision != expectedRevision )
			{
				return Task.FromResult( new StorageOutcome( StorageStatus.Conflict ) );
			}

			AuditFormat.ReadPayload( current.Document.PayloadJson );
			var names = Names( directory );
			var commits = names.Where( name => name.StartsWith( "c-", StringComparison.Ordinal ) )
				.OrderByDescending( StoreFormat.RevisionFromName ).ToArray();
			var retained = commits.Take( 2 ).ToArray();
			foreach ( var name in retained )
			{
				AuditFormat.ReadPayload( Decode( directory, name ).PayloadJson );
			}

			var cutoff = retained.Min( StoreFormat.RevisionFromName );
			var damaged = false;
			foreach ( var marker in commits.Skip( 2 ).Reverse().Take( 64 ) )
			{
				cancellation.ThrowIfCancellationRequested();
				try
				{
					AuditFormat.ReadPayload( Decode( directory, marker ).PayloadJson );
				}
				catch ( Exception error ) when ( IsDataError( error ) || error is UnsupportedStoreSchemaException )
				{
					damaged = true;
					continue;
				}

				// Retire the old marker first; an interruption leaves an ignored orphan, never a broken current commit.
				DeleteVerified( retention, $"{directory}/{marker}" );
				DeleteVerified( retention, $"{directory}/g-{marker.Substring( 2 )}" );
				removed++;
			}

			var remaining = Names( directory );
			foreach ( var orphan in remaining.Where( name => name.StartsWith( "g-", StringComparison.Ordinal )
				&& StoreFormat.RevisionFromName( name ) < cutoff
				&& !remaining.Contains( "c-" + name.Substring( 2 ), StringComparer.Ordinal ) ).Take( 64 - removed ) )
			{
				cancellation.ThrowIfCancellationRequested();
				// A candidate older than both retained commits has no commit marker and is never authoritative.
				// This also finishes cleanup interrupted after retiring an old marker.
				DeleteVerified( retention, $"{directory}/{orphan}" );
				removed++;
			}

			pending = Names( directory ).Any( name => StoreFormat.RevisionFromName( name ) < cutoff );
			return Task.FromResult( new StorageOutcome( damaged ? StorageStatus.Corrupt : StorageStatus.Committed ) );
		}, cancellation );
		return new StoragePruneResult( result.Status, removed, pending || result.Status != StorageStatus.Committed );
	}

	private void DeleteVerified( IStoreRetentionFiles files, string path )
	{
		if ( !_available() )
		{
			throw new IOException( "Retention runtime is unavailable." );
		}

		files.DeleteFile( path );
		if ( files.FileExists( path ) )
		{
			throw new IOException( "Audit retention deletion did not complete." );
		}
	}
}
