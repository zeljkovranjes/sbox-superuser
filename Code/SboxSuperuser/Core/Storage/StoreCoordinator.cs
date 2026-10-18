using System.Text.Json;

namespace SboxSuperuser.Core.Storage;

internal sealed partial class StoreCoordinator
{
	private readonly IStoreFiles _files;
	private readonly Func<bool> _available;
	private readonly SemaphoreSlim _gate = new( 1, 1 );

	internal StorageStatus LastStatus { get; private set; } = StorageStatus.Unknown;

	internal StoreCoordinator( IStoreFiles files, Func<bool> available )
	{
		_files = files;
		_available = available;
	}

	internal async Task<StorageOutcome> ReadAsync( string directory, int schema, CancellationToken cancellation )
	{
		return await SerializedAsync( () => Task.FromResult( ReadCurrent( directory, schema ) ), cancellation );
	}

	private async Task<StorageOutcome> SerializedAsync( Func<Task<StorageOutcome>> operation, CancellationToken cancellation )
	{
		var entered = false;
		try
		{
			if ( !_available() ) return Record( new StorageOutcome( StorageStatus.Unavailable ) );
			await _gate.WaitAsync( cancellation );
			entered = true;
			cancellation.ThrowIfCancellationRequested();
			if ( !_available() ) return Record( new StorageOutcome( StorageStatus.Unavailable ) );
			return Record( await operation() );
		}
		catch ( OperationCanceledException ) { return Record( new StorageOutcome( StorageStatus.Cancelled ) ); }
		catch ( UnsupportedStoreSchemaException ) { return Record( new StorageOutcome( StorageStatus.UnsupportedSchema ) ); }
		catch ( Exception error ) when ( IsDataError( error ) ) { return Record( new StorageOutcome( StorageStatus.Corrupt ) ); }
		catch ( IOException ) { return Record( new StorageOutcome( StorageStatus.Unavailable ) ); }
		catch ( UnauthorizedAccessException ) { return Record( new StorageOutcome( StorageStatus.Unavailable ) ); }
		finally
		{
			if ( entered ) _gate.Release();
		}
	}

	private StorageOutcome Record( StorageOutcome outcome )
	{
		LastStatus = outcome.Status;
		return outcome;
	}

	private StorageOutcome ReadCurrent( string directory, int schema )
	{
		if ( schema != 1 ) return new StorageOutcome( StorageStatus.UnsupportedSchema );
		if ( !_files.DirectoryExists( directory ) ) return new StorageOutcome( StorageStatus.Missing );
		var names = Names( directory );
		var commits = names.Where( name => name.StartsWith( "c-", StringComparison.Ordinal ) )
			.OrderByDescending( StoreFormat.RevisionFromName ).ToArray();
		if ( commits.Length == 0 ) return new StorageOutcome( StorageStatus.Corrupt );
		var revision = StoreFormat.RevisionFromName( commits[ 0 ] );
		if ( commits.Count( name => StoreFormat.RevisionFromName( name ) == revision ) != 1 )
		{
			return new StorageOutcome( StorageStatus.Corrupt );
		}

		return new StorageOutcome( StorageStatus.Found, Decode( directory, commits[ 0 ] ) );
	}

	private IReadOnlyList<string> Names( string directory )
	{
		var names = _files.ListFiles( directory );
		foreach ( var name in names )
		{
			if ( !name.StartsWith( "g-", StringComparison.Ordinal ) && !name.StartsWith( "c-", StringComparison.Ordinal ) )
			{
				throw new FormatException( "Unexpected file in storage directory." );
			}

			StoreFormat.RevisionFromName( name );
		}

		return names;
	}

	private StoredRevision Decode( string directory, string commitName )
	{
		var generationPath = $"{directory}/g-{commitName.Substring( 2 )}";
		if ( !_files.FileExists( generationPath ) ) throw new FormatException( "Committed generation is missing." );
		return StoreFormat.Decode( commitName, _files.Read( $"{directory}/{commitName}" ), _files.Read( generationPath ) );
	}

	private static bool IsDataError( Exception error ) => error is JsonException or FormatException
		or ArgumentException or InvalidOperationException or OverflowException or KeyNotFoundException;
}
