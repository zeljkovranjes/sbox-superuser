using SboxSuperuser.Core.Storage;
using SboxSuperuser.Engine.Audit;

namespace SboxSuperuser.Contracts.Tests;

internal sealed class AuditStoreHarness : IAdminStore, IAuditRetention, IStoreRetentionFiles
{
	internal MemoryStoreFiles Files { get; } = new();
	internal StoreCoordinator Coordinator { get; }
	internal Func<StoreWriteRequest, Task<StoreWriteStatus?>> BeforeWrite;
	internal Action<string> BeforeDelete;
	internal int Writes;
	internal int Prunes;
	internal AuditStoreHarness() => Coordinator = new( this, () => true );

	public async Task<StoreReadResult> ReadAsync( StoreKey key, int schema, CancellationToken cancellation )
	{
		var result = await Coordinator.ReadAsync( Path( key ), schema, cancellation );
		return new StoreReadResult( Enum.Parse<StoreReadStatus>( result.Status.ToString() ), result.Document is null ? null
			: new VersionedStoreDocument( result.Document.SchemaVersion, result.Document.Revision, result.Document.PayloadJson ) );
	}

	public async Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation )
	{
		Writes++;
		var injected = BeforeWrite is null ? null : await BeforeWrite( request );
		if ( injected.HasValue )
		{
			return new StoreWriteResult( injected.Value );
		}

		var result = await Coordinator.WriteAsync( Path( request.Key ), 1, request.ExpectedRevision, request.Document.PayloadJson, cancellation );
		return new StoreWriteResult( Enum.Parse<StoreWriteStatus>( result.Status.ToString() ), result.Status == StorageStatus.Committed ? result.Document.Revision : null );
	}

	public Task<StoragePruneResult> PruneAuditAsync( long revision, CancellationToken cancellation )
	{
		Prunes++;
		return Coordinator.PruneAuditHistoryAsync( "audit/journal", revision, cancellation );
	}

	private static string Path( StoreKey key ) => key.ModuleId + "/" + key.DocumentId;
	public bool DirectoryExists( string path ) => Files.DirectoryExists( path );
	public bool FileExists( string path ) => Files.FileExists( path );
	public void CreateDirectory( string path ) => Files.CreateDirectory( path );
	public IReadOnlyList<string> ListFiles( string path ) => Files.ListFiles( path );
	public string Read( string path ) => Files.Read( path );
	public Task WriteNewAsync( string path, string content, CancellationToken cancellation ) => Files.WriteNewAsync( path, content, cancellation );
	public void DeleteFile( string path )
	{
		BeforeDelete?.Invoke( path );
		Files.Files.Remove( path );
	}
}
