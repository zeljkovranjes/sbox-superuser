namespace SboxSuperuser.Core.Storage;

internal interface IStoreFiles
{
	bool DirectoryExists( string directory );
	void CreateDirectory( string directory );
	IReadOnlyList<string> ListFiles( string directory );
	string Read( string path );
	Task WriteNewAsync( string path, string content, CancellationToken cancellation );
}
