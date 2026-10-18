using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Contracts.Tests;

internal sealed class MemoryStoreFiles : IStoreFiles
{
	internal readonly Dictionary<string, string> Files = new();
	internal readonly HashSet<string> Directories = new();
	internal Func<string, string, CancellationToken, Task> WriteHook;
	internal Func<string, string> ReadHook;

	public bool DirectoryExists( string directory ) => Directories.Contains( directory );
	public bool FileExists( string path ) => Files.ContainsKey( path );
	public void CreateDirectory( string directory ) => Directories.Add( directory );
	public IReadOnlyList<string> ListFiles( string directory ) => Files.Keys
		.Where( path => path.StartsWith( directory + "/", StringComparison.Ordinal ) )
		.Select( path => path.Substring( directory.Length + 1 ) ).ToArray();
	public string Read( string path ) => ReadHook is null ? Files[ path ] : ReadHook( path );

	public async Task WriteNewAsync( string path, string content, CancellationToken cancellation )
	{
		cancellation.ThrowIfCancellationRequested();
		if ( Files.ContainsKey( path ) )
		{
			throw new IOException( "Refuse overwrite." );
		}

		if ( WriteHook is not null )
		{
			await WriteHook( path, content, cancellation );
		}
		else
		{
			Files.Add( path, content );
		}
	}
}
