using System.Text.Json;
using SboxSuperuser.Core.Storage;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class StorageTests
{
	private const string Directory = "test/policy";
	private static readonly CancellationToken NoCancellation = CancellationToken.None;
	private static AuditIntent Intent() => new( Guid.NewGuid(), Guid.NewGuid(), ActorKind.InternalService,
		"test.storage", CommandOrigin.InternalService, "test.change", DateTimeOffset.UtcNow );
	private static string Payload( string state = "{\"bans\":[\"76561198000000001\"],\"owners\":[\"76561198000000002\"]}" )
		=> new PolicyTransaction( state, new[] { Intent() } ).CreateWriteRequest( new StoreKey( "test", "policy" ), 0 ).Document.PayloadJson;
	private static StoreCoordinator Store( MemoryStoreFiles files ) => new( files, () => true );
	private static Task<StorageOutcome> Write( StoreCoordinator store, long expected, string payload = null )
		=> store.WriteAsync( Directory, 1, expected, payload ?? Payload(), NoCancellation );

	[Fact]
	public async Task RestartReadsExactStateAndUnknownIntentFromCommittedFiles()
	{
		var files = new MemoryStoreFiles();
		var payload = Payload();
		Assert.Equal( StorageStatus.Committed, (await Write( Store( files ), 0, payload )).Status );
		var restarted = await Store( files ).ReadAsync( Directory, 1, NoCancellation );
		Assert.Equal( StorageStatus.Found, restarted.Status );
		Assert.Equal( payload, restarted.Document.PayloadJson );
		Assert.Equal( 1, restarted.Document.Revision );
		using var json = JsonDocument.Parse( payload );
		Assert.Equal( "Unknown", json.RootElement.GetProperty( "auditIntents" )[ 0 ].GetProperty( "outcome" ).GetString() );
	}

	[Fact]
	public async Task MissingEmptyAndTruncatedFirstInstallationRemainDistinct()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		Assert.Equal( StorageStatus.Missing, (await store.ReadAsync( Directory, 1, NoCancellation )).Status );
		files.CreateDirectory( Directory );
		Assert.Equal( StorageStatus.Corrupt, (await store.ReadAsync( Directory, 1, NoCancellation )).Status );
		Assert.Equal( StorageStatus.Corrupt, (await Write( store, 0 )).Status );
		Assert.Empty( files.Files );
	}

	[Theory]
	[InlineData( false, false )]
	[InlineData( false, true )]
	[InlineData( true, false )]
	[InlineData( true, true )]
	public async Task InterruptedWritesPreserveBackupsAndNeverReportSuccess( bool atCommit, bool cancelled )
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		var initial = await Write( store, 0 );
		var baseline = new Dictionary<string, string>( files.Files );
		files.WriteHook = (path, content, token) =>
		{
			if ( path.Contains( atCommit ? "/c-" : "/g-" ) )
			{
				files.Files.Add( path, content.Substring( 0, content.Length / 2 ) );
				if ( cancelled ) throw new OperationCanceledException();
				throw new IOException( "Injected interruption." );
			}
			files.Files.Add( path, content );
			return Task.CompletedTask;
		};
		var result = await Write( store, 1 );
		Assert.Equal( atCommit ? StorageStatus.Unknown : cancelled ? StorageStatus.Cancelled : StorageStatus.Unavailable, result.Status );
		Assert.Null( result.Document );
		foreach ( var pair in baseline ) Assert.Equal( pair.Value, files.Files[ pair.Key ] );
		files.WriteHook = null;
		var restarted = Store( files );
		Assert.Equal( atCommit ? StorageStatus.Corrupt : StorageStatus.Found,
			(await restarted.ReadAsync( Directory, 1, NoCancellation )).Status );
		var backups = await restarted.GetBackupsAsync( Directory, NoCancellation );
		Assert.Contains( backups.Documents, doc => doc.GenerationId == initial.Document.GenerationId );
		var recovery = await restarted.RecoverAsync( Directory, initial.Document.GenerationId,
			backups.HeadRevision, Intent().ToJson(), NoCancellation );
		Assert.Equal( StorageStatus.Committed, recovery.Status );
		Assert.Equal( 3, recovery.Document.Revision );
		Assert.Contains( "76561198000000001", recovery.Document.PayloadJson );
		Assert.Contains( "76561198000000002", recovery.Document.PayloadJson );
		using var restored = JsonDocument.Parse( recovery.Document.PayloadJson );
		Assert.Equal( 2, restored.RootElement.GetProperty( "auditIntents" ).GetArrayLength() );
		foreach ( var pair in baseline ) Assert.Equal( pair.Value, files.Files[ pair.Key ] );
	}

	[Fact]
	public async Task CommitReadbackFailureReturnsUnknownEvenIfRestartFindsCommittedData()
	{
		var files = new MemoryStoreFiles();
		files.ReadHook = path => path.Contains( "/c-" ) ? throw new IOException( "Read failure." ) : files.Files[ path ];
		Assert.Equal( StorageStatus.Unknown, (await Write( Store( files ), 0 )).Status );
		files.ReadHook = null;
		Assert.Equal( StorageStatus.Found, (await Store( files ).ReadAsync( Directory, 1, NoCancellation )).Status );
		Assert.Equal( StorageStatus.Conflict, (await Write( Store( files ), 0 )).Status );
	}

	[Fact]
	public async Task ConcurrentWritesSerializeAndOnlyOneWinsExpectedRevision()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		var started = new TaskCompletionSource();
		var resume = new TaskCompletionSource();
		files.WriteHook = async (path, content, token) =>
		{
			started.TrySetResult();
			await resume.Task;
			files.Files.Add( path, content );
		};
		var first = Write( store, 0 );
		await started.Task;
		var second = Write( store, 0 );
		Assert.False( first.IsCompleted );
		Assert.False( second.IsCompleted );
		resume.SetResult();
		Assert.Equal( StorageStatus.Committed, (await first).Status );
		Assert.Equal( StorageStatus.Conflict, (await second).Status );
		Assert.Equal( 2, files.Files.Count );
	}

	[Fact]
	public async Task RetirementDuringCandidateWritePreventsCommitAndQueuedMutation()
	{
		var files = new MemoryStoreFiles();
		var available = true;
		var store = new StoreCoordinator( files, () => available );
		files.WriteHook = (path, content, token) =>
		{
			files.Files.Add( path, content );
			available = false;
			return Task.CompletedTask;
		};
		Assert.Equal( StorageStatus.Unavailable, (await Write( store, 0 )).Status );
		Assert.DoesNotContain( files.Files.Keys, path => path.Contains( "/c-" ) );
		Assert.Equal( StorageStatus.Unavailable, (await Write( store, 0 )).Status );
		Assert.Equal( StorageStatus.Unavailable, (await store.ReadAsync( Directory, 1, NoCancellation )).Status );
	}

	[Theory]
	[InlineData( "generation" )]
	[InlineData( "commit" )]
	[InlineData( "missing" )]
	[InlineData( "digest" )]
	public async Task DamagedNewestCommitCannotResetOwnersOrBans( string corruption )
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		await Write( store, 0 );
		await Write( store, 1 );
		var generation = files.Files.Keys.Single( path => path.Contains( "/g-00000000000000000002-" ) );
		var commit = generation.Replace( "/g-", "/c-" );
		switch ( corruption )
		{
			case "generation": files.Files[ generation ] = "{"; break;
			case "commit": files.Files[ commit ] = "{"; break;
			case "missing": files.Files.Remove( generation ); break;
			case "digest": files.Files[ generation ] = files.Files[ generation ].Replace( "76561198000000001", "76561198000000009" ); break;
		}
		var count = files.Files.Count;
		var read = await store.ReadAsync( Directory, 1, NoCancellation );
		Assert.Equal( StorageStatus.Corrupt, read.Status );
		Assert.Null( read.Document );
		Assert.Equal( StorageStatus.Corrupt, (await Write( store, 0 )).Status );
		Assert.Equal( StorageStatus.Corrupt, (await Write( store, 2 )).Status );
		Assert.Equal( count, files.Files.Count );
		Assert.Single( (await store.GetBackupsAsync( Directory, NoCancellation )).Documents );
	}

	[Fact]
	public async Task UnsupportedUpgradePreservesReadableVersionAndFiles()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		var original = await Write( store, 0 );
		Assert.Equal( StorageStatus.UnsupportedSchema, (await store.WriteAsync( Directory, 2, 1, Payload(), NoCancellation )).Status );
		Assert.Equal( original.Document, (await store.ReadAsync( Directory, 1, NoCancellation )).Document );
		Assert.Equal( 2, files.Files.Count );
		var future = new StoredRevision( Guid.NewGuid(), 2, 2, Payload() );
		var generation = StoreFormat.Generation( future );
		files.Files.Add( $"{Directory}/g-{StoreFormat.Stem( future )}.json", generation );
		files.Files.Add( $"{Directory}/c-{StoreFormat.Stem( future )}.json", StoreFormat.Commit( future, generation ) );
		Assert.Equal( StorageStatus.UnsupportedSchema, (await store.ReadAsync( Directory, 1, NoCancellation )).Status );
		Assert.Equal( StorageStatus.UnsupportedSchema, (await Write( store, 0 )).Status );
		var backups = await store.GetBackupsAsync( Directory, NoCancellation );
		Assert.Single( backups.Documents );
		Assert.Equal( StorageStatus.Committed, (await store.RecoverAsync( Directory, original.Document.GenerationId, 2, Intent().ToJson(), NoCancellation )).Status );
	}

	[Fact]
	public async Task RecoveryRefusesChangedHeadAndInvalidBackup()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		var first = await Write( store, 0 );
		await Write( store, 1 );
		Assert.Equal( StorageStatus.Conflict, (await store.RecoverAsync( Directory, first.Document.GenerationId, 1, Intent().ToJson(), NoCancellation )).Status );
		Assert.Equal( StorageStatus.InvalidData, (await store.RecoverAsync( Directory, Guid.NewGuid(), 2, Intent().ToJson(), NoCancellation )).Status );
		Assert.Equal( 4, files.Files.Count );
	}

	[Fact]
	public async Task CancelledBeforeWriteCreatesNoFilesOrDirectory()
	{
		var files = new MemoryStoreFiles();
		var result = await Store( files ).WriteAsync( Directory, 1, 0, Payload(), new CancellationToken( true ) );
		Assert.Equal( StorageStatus.Cancelled, result.Status );
		Assert.Empty( files.Files );
		Assert.Empty( files.Directories );
	}

	[Fact]
	public async Task AmbiguousHighestRevisionFailsClosed()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		await Write( store, 0 );
		var duplicate = new StoredRevision( Guid.NewGuid(), 1, 1, Payload() );
		var generation = StoreFormat.Generation( duplicate );
		files.Files.Add( $"{Directory}/g-{StoreFormat.Stem( duplicate )}.json", generation );
		files.Files.Add( $"{Directory}/c-{StoreFormat.Stem( duplicate )}.json", StoreFormat.Commit( duplicate, generation ) );
		Assert.Equal( StorageStatus.Corrupt, (await store.ReadAsync( Directory, 1, NoCancellation )).Status );
		Assert.Equal( StorageStatus.Corrupt, (await Write( store, 0 )).Status );
		Assert.Equal( 2, (await store.GetBackupsAsync( Directory, NoCancellation )).Documents.Count );
	}

	[Fact]
	public async Task NoValidBackupNeverBecomesMissingOrEmptyPolicy()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		await Write( store, 0 );
		await Write( store, 1 );
		foreach ( var path in files.Files.Keys.ToArray() ) files.Files[ path ] = "{broken";
		var backups = await store.GetBackupsAsync( Directory, NoCancellation );
		Assert.Equal( StorageStatus.Corrupt, backups.Status );
		Assert.Empty( backups.Documents );
		Assert.Equal( StorageStatus.Corrupt, (await Write( store, 0 )).Status );
		Assert.Equal( 4, files.Files.Count );
		Assert.All( files.Files.Values, text => Assert.Equal( "{broken", text ) );
	}

	[Fact]
	public async Task BackupInspectionIsBoundedWithoutPruningHistory()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		for ( var revision = 0; revision < 101; revision++ )
		{
			Assert.Equal( StorageStatus.Committed, (await Write( store, revision )).Status );
		}
		var backups = await store.GetBackupsAsync( Directory, NoCancellation );
		Assert.Equal( 100, backups.Documents.Count );
		Assert.True( backups.HasMore );
		Assert.Equal( 101, backups.HeadRevision );
		Assert.Equal( 101, backups.Documents[ 0 ].Revision );
		Assert.Equal( 202, files.Files.Count );
	}

	[Fact]
	public async Task OversizedPayloadAndPreCancelledQueuedWriteDoNotMutateStore()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		Assert.Equal( StorageStatus.InvalidData, (await Write( store, 0, new string( 'x', StoreFormat.MaximumBytes ) )).Status );
		Assert.Empty( files.Files );
		var started = new TaskCompletionSource();
		var resume = new TaskCompletionSource();
		files.WriteHook = async (path, content, token) =>
		{
			started.TrySetResult();
			await resume.Task;
			files.Files.Add( path, content );
		};
		var first = Write( store, 0 );
		await started.Task;
		using var cancel = new CancellationTokenSource();
		var queued = store.WriteAsync( Directory, 1, 0, Payload(), cancel.Token );
		cancel.Cancel();
		Assert.Equal( StorageStatus.Cancelled, (await queued).Status );
		resume.SetResult();
		Assert.Equal( StorageStatus.Committed, (await first).Status );
		Assert.Equal( 2, files.Files.Count );
	}

	[Fact]
	public void TransactionSnapshotsAuditInputsAndRejectsNonUtcTimes()
	{
		var fields = new Dictionary<string, string> { [ "reason" ] = "original" };
		var targets = new[] { "76561198000000001" };
		var intent = new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), ActorKind.DedicatedConsole,
			"dedicated-console", CommandOrigin.Console, "test.change", DateTimeOffset.UtcNow, fields, targets );
		fields[ "reason" ] = "modified";
		targets[ 0 ] = "76561198000000002";
		var payload = new PolicyTransaction( "{}", new[] { intent } ).CreateWriteRequest( new StoreKey( "test", "policy" ), 0 ).Document.PayloadJson;
		Assert.Contains( "original", payload );
		Assert.DoesNotContain( "modified", payload );
		Assert.Contains( "76561198000000001", payload );
		Assert.DoesNotContain( "76561198000000002", payload );
		Assert.Throws<ArgumentException>( () => new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), ActorKind.DedicatedConsole,
			"dedicated-console", CommandOrigin.Console, "test.change", DateTimeOffset.Now.ToOffset( TimeSpan.FromHours( 1 ) ) ) );
	}

	[Fact]
	public async Task MalformedSchemaAndDuplicatePropertiesNeverReachStorage()
	{
		var files = new MemoryStoreFiles();
		var store = Store( files );
		var candidates = new[] { "{}", "{", Payload().Replace( "Unknown", "Success" ),
			"{\"state\":{\"x\":1,\"x\":2},\"auditIntents\":[" + Intent().ToJson() + "]}",
			"{\"state\":{},\"auditIntents\":[]}" };
		foreach ( var candidate in candidates )
		{
			Assert.Equal( StorageStatus.InvalidData, (await Write( store, 0, candidate )).Status );
		}
		Assert.Empty( files.Files );
		Assert.Throws<ArgumentException>( () => new PolicyTransaction( "[]", new[] { Intent() } ) );
		Assert.Throws<ArgumentException>( () => new AuditIntent( Guid.Empty, Guid.NewGuid(), ActorKind.DedicatedConsole,
			"dedicated-console", CommandOrigin.Console, "test.change", DateTimeOffset.UtcNow ) );
	}
}
