using SboxSuperuser.Engine.Storage;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ConfiguredStoreTests
{
	private sealed class Backend : IAdminStorageBackend
	{
		internal Func<Task<StoreWriteResult>> Write;
		internal StoreReadResult Read = new( StoreReadStatus.Missing );
		internal int Calls;
		public Task<StoreReadResult> ReadAsync( StoreKey key, int schema, CancellationToken cancellation ) => Task.FromResult( Read );
		public Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation ) { Calls++; return Write(); }
		public Task<StoreBackupList> GetBackupsAsync( StoreKey key, CancellationToken cancellation )
			=> Task.FromResult( new StoreBackupList( StoreReadStatus.Missing, 0, Array.Empty<StoreBackup>(), false ) );
		public Task<StoreWriteResult> RecoverAsync( StoreKey key, Guid generation, long revision, AuditIntent intent, CancellationToken cancellation ) => Write();
		public Task<StoreRetentionResult> PruneAuditHistoryAsync( long revision, CancellationToken cancellation )
			=> Task.FromResult( new StoreRetentionResult( StoreWriteStatus.Committed ) );
	}

	private static StoreWriteRequest Request()
	{
		var intent = new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), ActorKind.Player, DispatcherRig.OwnerId,
			CommandOrigin.Interface, "test.write", DateTimeOffset.UtcNow );
		return new PolicyTransaction( "{}", new[] { intent } ).CreateWriteRequest( new StoreKey( "example", "state" ), 0 );
	}

	[Theory]
	[InlineData( StoreWriteStatus.Committed )]
	[InlineData( StoreWriteStatus.Unknown )]
	[InlineData( StoreWriteStatus.Conflict )]
	[InlineData( StoreWriteStatus.Corrupt )]
	public async Task UncertainOrSuccessfulCustomWritesInvalidateCachedAuthority( StoreWriteStatus status )
	{
		var backend = new Backend { Write = () => Task.FromResult( new StoreWriteResult( status, status == StoreWriteStatus.Committed ? 1 : null ) ) };
		var store = new ConfiguredAdminStore( backend, () => true, _ => { } );
		var changes = 0;
		store.StorageChanged += _ => changes++;
		Assert.Equal( status, (await store.WriteAsync( Request(), default )).Status );
		Assert.Equal( 1, changes );
	}

	[Fact]
	public async Task ProviderExceptionOrWrongRevisionNeverClaimsCommit()
	{
		var backend = new Backend { Write = () => throw new OperationCanceledException() };
		var store = new ConfiguredAdminStore( backend, () => true, _ => { } );
		Assert.Equal( StoreWriteStatus.Unknown, (await store.WriteAsync( Request(), default )).Status );
		backend.Write = () => Task.FromResult( new StoreWriteResult( StoreWriteStatus.Committed, 9 ) );
		Assert.Equal( StoreWriteStatus.Unknown, (await store.WriteAsync( Request(), default )).Status );
	}

	[Fact]
	public async Task RetiredRuntimeAndPrecancelledWritesNeverCallBackend()
	{
		var backend = new Backend();
		var active = false;
		var store = new ConfiguredAdminStore( backend, () => active, _ => { } );
		Assert.Equal( StoreWriteStatus.Unavailable, (await store.WriteAsync( Request(), default )).Status );
		active = true;
		Assert.Equal( StoreWriteStatus.Cancelled, (await store.WriteAsync( Request(), new CancellationToken( true ) )).Status );
		Assert.Equal( 0, backend.Calls );
	}

	[Fact]
	public async Task NullProviderAndInvalidPayloadFailClosed()
	{
		var missing = new ConfiguredAdminStore( null, () => true, _ => { } );
		Assert.Equal( StoreReadStatus.Unavailable, (await missing.ReadAsync( new StoreKey( "security", "policy" ), 1, default )).Status );
		var backend = new Backend { Read = new StoreReadResult( StoreReadStatus.Found, new VersionedStoreDocument( 1, 1, "not json" ) ) };
		var store = new ConfiguredAdminStore( backend, () => true, _ => { } );
		Assert.Equal( StoreReadStatus.Corrupt, (await store.ReadAsync( new StoreKey( "security", "policy" ), 1, default )).Status );
	}

	[Fact]
	public void BackupResultsRequireBoundedValidMetadata()
	{
		Assert.Throws<ArgumentException>( () => new StoreBackup( Guid.Empty, 1, 1 ) );
		Assert.Throws<ArgumentException>( () => new StoreBackupList( StoreReadStatus.Found, 1, new[] { new StoreBackup( Guid.NewGuid(), 2, 1 ) }, false ) );
	}
}
