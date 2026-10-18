namespace SboxSuperuser.Core.Storage;

internal interface IStoreRetentionFiles : IStoreFiles
{
	void DeleteFile( string path );
}
