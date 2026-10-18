using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Contracts.Tests;

// A storage-port double for policy orchestration tests. Real engine IO is covered by separate runtime scripts.
internal sealed class MemoryPolicyStore : IAdminStore
{
	internal readonly Dictionary<string, VersionedStoreDocument> Documents = new();
	internal readonly Dictionary<string, StoreReadStatus> ReadOverrides = new();
	internal Func<StoreWriteRequest, CancellationToken, Task> BeforeCommit;
	internal StoreWriteStatus? WriteOverride;
	internal bool ThrowOnRead;
	internal int WriteCount;

	internal static string Id( StoreKey key ) => key.ModuleId + "/" + key.DocumentId;

	public Task<StoreReadResult> ReadAsync( StoreKey key, int supportedSchemaVersion, CancellationToken cancellation )
	{
		if ( ThrowOnRead ) throw new IOException( "Injected storage failure." );
		cancellation.ThrowIfCancellationRequested();
		if ( ReadOverrides.TryGetValue( Id( key ), out var status ) ) return Task.FromResult( new StoreReadResult( status ) );
		if ( supportedSchemaVersion != 1 ) return Task.FromResult( new StoreReadResult( StoreReadStatus.UnsupportedSchema ) );
		return Task.FromResult( Documents.TryGetValue( Id( key ), out var document )
			? new StoreReadResult( StoreReadStatus.Found, document ) : new StoreReadResult( StoreReadStatus.Missing ) );
	}

	public async Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation )
	{
		WriteCount++;
		if ( WriteOverride.HasValue ) return new StoreWriteResult( WriteOverride.Value );
		TransactionFormat.Validate( request.Document.PayloadJson );
		Documents.TryGetValue( Id( request.Key ), out var current );
		if ( (current?.Revision ?? 0) != request.ExpectedRevision ) return new StoreWriteResult( StoreWriteStatus.Conflict );
		if ( BeforeCommit is not null ) await BeforeCommit( request, cancellation );
		cancellation.ThrowIfCancellationRequested();
		Documents[ Id( request.Key ) ] = request.Document;
		return new StoreWriteResult( StoreWriteStatus.Committed, request.Document.Revision );
	}
}
