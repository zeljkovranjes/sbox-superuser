using System.Text;
using Sandbox;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Storage;

internal sealed class EngineStoreFiles : IStoreFiles
{
	private readonly Func<bool> _available;

	// Trusted development scripts can pause a real write with its stream open. Never set by game inputs.
	internal Func<string, CancellationToken, Task> WriteCheckpoint { get; set; }

	internal EngineStoreFiles( Func<bool> available )
	{
		_available = available;
	}

	public bool DirectoryExists( string directory )
	{
		if ( FileExists( directory ) )
		{
			throw new FormatException( "Expected a storage directory, found a file." );
		}

		return Access( () => FileSystem.Data.DirectoryExists( directory ) );
	}

	public bool FileExists( string path ) => Access( () => FileSystem.Data.FileExists( path ) );

	public void CreateDirectory( string directory )
	{
		Access( () =>
		{
			FileSystem.Data.CreateDirectory( directory );
			return true;
		} );
	}

	public IReadOnlyList<string> ListFiles( string directory )
	{
		return Access( () => FileSystem.Data.FindFile( directory, "*", false )
			.Select( path => path.Replace( '\\', '/' ).Split( '/' ).Last() ).ToArray() );
	}

	public string Read( string path )
	{
		if ( Access( () => FileSystem.Data.FileSize( path ) ) > StoreFormat.MaximumBytes )
		{
			throw new FormatException( "Stored record exceeds the byte limit." );
		}

		return Access( () => FileSystem.Data.ReadAllText( path ) );
	}

	public async Task WriteNewAsync( string path, string content, CancellationToken cancellation )
	{
		try
		{
			await WriteStreamAsync( path, content, cancellation );
		}
		catch ( OperationCanceledException )
		{
			throw;
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Storage stream failed; type={error.GetType().Name}" );
			throw new IOException( "Host-local storage stream failed." );
		}
	}

	private async Task WriteStreamAsync( string path, string content, CancellationToken cancellation )
	{
		cancellation.ThrowIfCancellationRequested();
		var bytes = Encoding.UTF8.GetBytes( content );
		using var stream = Access( () => FileSystem.Data.OpenWrite( path, FileMode.CreateNew ) );
		var checkpoint = WriteCheckpoint;
		if ( checkpoint is not null )
		{
			stream.Write( bytes, 0, bytes.Length / 2 );
			stream.Flush();
			await checkpoint( path, cancellation );
			cancellation.ThrowIfCancellationRequested();
			EnsureAvailable();
			stream.Write( bytes, bytes.Length / 2, bytes.Length - bytes.Length / 2 );
		}
		else
		{
			stream.Write( bytes, 0, bytes.Length );
		}

		stream.Flush();
	}

	private void EnsureAvailable()
	{
		if ( !ThreadSafe.IsMainThread || !_available() )
		{
			throw new IOException( "Host storage runtime is unavailable." );
		}
	}

	private T Access<T>( Func<T> operation )
	{
		EnsureAvailable();
		try
		{
			return operation();
		}
		catch ( Exception error )
		{
			// Engine adapters may throw engine-specific IO errors. Preserve a safe failure classification.
			Log.Warning( $"[Superuser] Storage IO unavailable; failure type={error.GetType().Name}" );
			throw new IOException( "Host-local storage operation failed." );
		}
	}
}
