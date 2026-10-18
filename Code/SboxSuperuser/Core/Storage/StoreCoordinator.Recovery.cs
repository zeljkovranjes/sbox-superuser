using System.Text.Json;

namespace SboxSuperuser.Core.Storage;

internal sealed partial class StoreCoordinator
{
	internal async Task<StorageBackups> GetBackupsAsync( string directory, CancellationToken cancellation )
	{
		var backups = new List<StoredRevision>();
		long head = 0;
		var hasMore = false;
		var outcome = await SerializedAsync( () =>
		{
			if ( !_files.DirectoryExists( directory ) )
			{
				return Task.FromResult( new StorageOutcome( StorageStatus.Missing ) );
			}

			var names = Names( directory );
			head = names.Select( StoreFormat.RevisionFromName ).DefaultIfEmpty( 0 ).Max();
			foreach ( var name in names.Where( name => name.StartsWith( "c-", StringComparison.Ordinal ) )
				.OrderByDescending( StoreFormat.RevisionFromName ) )
			{
				cancellation.ThrowIfCancellationRequested();
				try
				{
					var document = Decode( directory, name );
					if ( backups.Count == 100 )
					{
						hasMore = true;
						break;
					}

					backups.Add( document );
				}
				catch ( UnsupportedStoreSchemaException ) { }
				catch ( Exception error ) when ( IsDataError( error ) ) { }
			}

			return Task.FromResult( ReadCurrent( directory, 1 ) );
		}, cancellation );
		return new StorageBackups( outcome.Status, head, backups.AsReadOnly(), hasMore );
	}

	internal async Task<StorageOutcome> RecoverAsync( string directory, Guid generationId,
		long expectedHeadRevision, string recoveryIntent, CancellationToken cancellation )
	{
		return await SerializedAsync( async () =>
		{
			if ( !_files.DirectoryExists( directory ) )
			{
				return new StorageOutcome( StorageStatus.Missing );
			}

			var names = Names( directory );
			var head = names.Select( StoreFormat.RevisionFromName ).DefaultIfEmpty( 0 ).Max();
			if ( head != expectedHeadRevision || head == long.MaxValue )
			{
				return new StorageOutcome( StorageStatus.Conflict );
			}

			var selected = names.SingleOrDefault( name => name.StartsWith( "c-", StringComparison.Ordinal )
				&& name.EndsWith( $"-{generationId:N}.json", StringComparison.Ordinal ) );
			if ( selected is null )
			{
				return new StorageOutcome( StorageStatus.InvalidData );
			}

			var backup = Decode( directory, selected );
			string payload;
			try
			{
				using var source = JsonDocument.Parse( backup.PayloadJson );
				var intents = source.RootElement.GetProperty( "auditIntents" ).EnumerateArray()
					.Select( item => item.GetRawText() ).Append( recoveryIntent );
				payload = "{\"state\":" + source.RootElement.GetProperty( "state" ).GetRawText()
					+ ",\"auditIntents\":[" + string.Join( ",", intents ) + "]}";
				TransactionFormat.Validate( payload );
			}
			catch ( Exception error ) when ( IsDataError( error ) )
			{
				return new StorageOutcome( StorageStatus.InvalidData );
			}

			return await CommitAsync( directory, new StoredRevision( Guid.NewGuid(), 1, head + 1, payload ), cancellation );
		}, cancellation );
	}
}
