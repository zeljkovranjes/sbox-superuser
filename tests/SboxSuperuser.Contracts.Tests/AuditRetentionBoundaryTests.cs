using SboxSuperuser.Core.Audit;
using SboxSuperuser.Core.Storage;
using SboxSuperuser.Engine.Audit;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AuditRetentionBoundaryTests
{
	private static async Task Fill( AuditStoreHarness store, int count )
	{
		var action = new AuditAction( DispatcherRig.Owner, CommandOrigin.Console, Guid.NewGuid(), "test.run", DateTimeOffset.UtcNow );
		var journal = new AuditJournal().Append( action.Record( 1 ) );
		var transaction = new PolicyTransaction( AuditFormat.WriteState( journal ), action.Intents );
		for ( var revision = 0; revision < count; revision++ )
		{
			Assert.Equal( StoreWriteStatus.Committed, (await store.WriteAsync(
				transaction.CreateWriteRequest( HostAuditService.Key, revision ), CancellationToken.None )).Status );
		}
	}

	[Fact]
	public async Task CleanupDeletesAtMost64GenerationsAndRejectsStaleRevision()
	{
		var store = new AuditStoreHarness();
		await Fill( store, 70 );
		Assert.Equal( StorageStatus.Conflict, (await store.PruneAuditAsync( 69, CancellationToken.None )).Status );
		Assert.Equal( 140, store.Files.Files.Count );
		var first = await store.PruneAuditAsync( 70, CancellationToken.None );
		Assert.Equal( 64, first.RemovedGenerations );
		Assert.True( first.HasMore );
		var second = await store.PruneAuditAsync( 70, CancellationToken.None );
		Assert.Equal( 4, second.RemovedGenerations );
		Assert.False( second.HasMore );
		Assert.Equal( 4, store.Files.Files.Count );
	}

	[Fact]
	public async Task DamagedCommittedHistoryIsPreservedWithDegradedRetention()
	{
		var store = new AuditStoreHarness();
		await Fill( store, 4 );
		var damaged = store.Files.Files.Keys.Where( key => key.Contains( "/g-" ) ).Order().First();
		store.Files.Files[ damaged ] = "damaged committed evidence";
		var result = await store.PruneAuditAsync( 4, CancellationToken.None );
		Assert.Equal( StorageStatus.Corrupt, result.Status );
		Assert.True( result.HasMore );
		Assert.Equal( "damaged committed evidence", store.Files.Files[ damaged ] );
		Assert.Equal( StoreReadStatus.Found, (await store.ReadAsync( HostAuditService.Key, 1, CancellationToken.None )).Status );
		Assert.Equal( 6, store.Files.Files.Count );
	}

	[Fact]
	public void EncodedByteCeilingTrimsBelowConfiguredCount()
	{
		var values = Enumerable.Range( 0, 32 ).ToDictionary( i => "argument" + i, _ => new string( '\u2665', 256 ) );
		var journal = new AuditJournal( 10 );
		for ( var sequence = 1; sequence <= 10; sequence++ )
		{
			journal = journal.Append( new AuditRecord( sequence, Guid.NewGuid(), Guid.NewGuid(), "Player", DispatcherRig.UserId,
				"Chat", "test.run", values, null, DateTimeOffset.UtcNow ) );
		}

		var trimmed = journal.Trim( DateTimeOffset.UtcNow, new HashSet<Guid>() );
		Assert.True( trimmed.Records.Count < 10 );
		Assert.True( System.Text.Encoding.UTF8.GetByteCount( AuditFormat.WriteState( trimmed ) ) <= AuditJournal.MaximumStateBytes );
	}
}
