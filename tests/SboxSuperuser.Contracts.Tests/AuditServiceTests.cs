using SboxSuperuser.Core.Audit;
using SboxSuperuser.Core.Storage;
using SboxSuperuser.Engine.Audit;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AuditServiceTests
{
	private static readonly CancellationToken None = CancellationToken.None;
	private static AuditAction Action( ActorIdentity actor = null, string command = "test.run" ) => new(
		actor ?? DispatcherRig.Owner, CommandOrigin.Console, Guid.NewGuid(), command, DateTimeOffset.UtcNow,
		targets: new[] { DispatcherRig.TargetId } );
	private static HostAuditService Service( AuditStoreHarness store, Func<DateTimeOffset> clock = null ) => new( store, () => true, _ => { }, clock );
	private static Task<AuditPage> Query( HostAuditService service, AuditQuery query = null ) => service.QueryAsync(
		() => DispatcherRig.Owner, _ => true, _ => false, query ?? new AuditQuery(), None );

	[Fact]
	public async Task IntentSurvivesRestartAsUnknownAndOutcomeIsCorrelated()
	{
		var store = new AuditStoreHarness();
		var service = Service( store );
		var action = Action();
		Assert.True( await service.BeginAsync( action, None ) );
		var restarted = Service( store );
		var unresolved = Assert.Single( (await Query( restarted )).Entries );
		Assert.Equal( action.Id, unresolved.Id );
		Assert.Equal( CommandStatus.Unknown, unresolved.Outcome );
		Assert.Null( unresolved.CompletedUtc );
		Assert.True( await service.CompleteAsync( action, CommandStatus.Success, None ) );
		var completed = Assert.Single( (await Query( restarted )).Entries );
		Assert.Equal( unresolved.RequestId, completed.RequestId );
		Assert.Equal( CommandStatus.Success, completed.Outcome );
		Assert.Equal( DispatcherRig.OwnerId, completed.ActorSubject );
		Assert.Equal( DispatcherRig.TargetId, Assert.Single( completed.Targets ) );
	}

	[Theory]
	[InlineData( StoreWriteStatus.Unavailable )]
	[InlineData( StoreWriteStatus.Unknown )]
	[InlineData( StoreWriteStatus.Conflict )]
	public async Task FailedOutcomeNeverTurnsIntentIntoSuccess( StoreWriteStatus failure )
	{
		var store = new AuditStoreHarness();
		var service = Service( store );
		var action = Action();
		Assert.True( await service.BeginAsync( action, None ) );
		store.BeforeWrite = _ => Task.FromResult<StoreWriteStatus?>( failure );
		Assert.False( await service.CompleteAsync( action, CommandStatus.Success, None ) );
		Assert.True( service.Diagnostics().IsDegraded );
		Assert.Equal( CommandStatus.Unknown, Assert.Single( (await Query( Service( store ) )).Entries ).Outcome );
	}

	[Fact]
	public async Task FiltersAndRevisionBoundPaginationCannotExposeOtherPlayers()
	{
		var service = Service( new AuditStoreHarness() );
		await service.RecordAsync( Action(), CommandStatus.Denied, None );
		var wanted = Action( command: "test.other" );
		await service.RecordAsync( wanted, CommandStatus.Success, None );
		await service.RecordAsync( Action( DispatcherRig.User ), CommandStatus.Failed, None );
		var first = await Query( service, new AuditQuery( pageSize: 1 ) );
		var second = await Query( service, new AuditQuery( pageSize: 1, cursor: first.NextCursor ) );
		Assert.Equal( wanted.Id, Assert.Single( second.Entries ).Id );
		Assert.Single( (await Query( service, new AuditQuery( command: "test.other", outcome: CommandStatus.Success,
			origin: CommandOrigin.Console, targetSteamId: DispatcherRig.TargetId, actorSubject: DispatcherRig.OwnerId ) )).Entries );
		var own = await service.QueryAsync( () => DispatcherRig.User, _ => false, _ => true, new AuditQuery(), None );
		Assert.Equal( DispatcherRig.UserId, Assert.Single( own.Entries ).ActorSubject );
		Assert.Equal( CommandStatus.Denied, (await service.QueryAsync( () => DispatcherRig.User, _ => false, _ => true,
			new AuditQuery( actorSubject: DispatcherRig.OwnerId ), None )).Status );
		await service.RecordAsync( Action(), CommandStatus.NoChange, None );
		Assert.Equal( CommandStatus.Conflict, (await Query( service, new AuditQuery( cursor: first.NextCursor ) )).Status );
	}

	[Fact]
	public async Task QueryRechecksIdentityAndPermissionAfterStorageRead()
	{
		var store = new AuditStoreHarness();
		var service = Service( store );
		await service.RecordAsync( Action(), CommandStatus.Success, None );
		var authorized = true;
		store.Files.ReadHook = path => { authorized = false; return store.Files.Files[ path ]; };
		var result = await service.QueryAsync( () => authorized ? DispatcherRig.Owner : null, _ => authorized, _ => false, new AuditQuery(), None );
		Assert.Equal( CommandStatus.Denied, result.Status );
		Assert.Empty( result.Entries );
	}

	[Fact]
	public async Task RetentionCompactsBothRemainingGenerationsAndPreservesActiveIntent()
	{
		var store = new AuditStoreHarness();
		var service = Service( store );
		var active = Action();
		await service.BeginAsync( active, None );
		await service.RecordAsync( Action(), CommandStatus.Denied, None );
		Assert.Equal( CommandStatus.Success, (await service.ConfigureAsync( () => DispatcherRig.Owner, new AuditRetentionOptions( 2 ), None )).Status );
		Assert.Equal( 4, store.Files.Files.Count );
		foreach ( var content in store.Files.Files.Where( pair => pair.Key.Contains( "/g-" ) ).Select( pair => pair.Value ) )
		{
			Assert.Contains( active.Id.ToString(), content );
		}
		Assert.Equal( 2, (await Query( service )).Entries.Count );
		Assert.True( await service.CompleteAsync( active, CommandStatus.Success, None ) );
	}

	[Fact]
	public async Task InterruptedSecondRetentionCheckpointResumesOnRestart()
	{
		var store = new AuditStoreHarness();
		var service = Service( store );
		await service.RecordAsync( Action(), CommandStatus.Success, None );
		await service.RecordAsync( Action(), CommandStatus.Denied, None );
		var writes = 0;
		store.BeforeWrite = _ => Task.FromResult<StoreWriteStatus?>( ++writes == 2 ? StoreWriteStatus.Unavailable : null );
		Assert.Equal( CommandStatus.Unknown, (await service.ConfigureAsync( () => DispatcherRig.Owner, new AuditRetentionOptions( 1 ), None )).Status );
		Assert.True( service.Diagnostics().RetentionPending );
		store.BeforeWrite = null;
		var restarted = Service( store );
		await restarted.Initialization;
		Assert.False( restarted.Diagnostics().IsDegraded );
		Assert.Single( (await Query( restarted )).Entries );
		Assert.Equal( 4, store.Files.Files.Count );
		var saved = await store.ReadAsync( HostAuditService.Key, 1, None );
		Assert.False( AuditFormat.ReadPayload( saved.Document.PayloadJson ).NeedsRetentionCheckpoint );
	}

	[Fact]
	public async Task ExpiryRunsAtRestartAndQueryDoesNotReturnExpiredRows()
	{
		var store = new AuditStoreHarness();
		var now = DateTimeOffset.UtcNow;
		var service = Service( store, () => now );
		await service.RecordAsync( Action(), CommandStatus.Success, None );
		now = now.AddDays( 31 );
		Assert.Empty( (await Query( service )).Entries );
		var restarted = Service( store, () => now );
		await restarted.Initialization;
		Assert.Empty( (await Query( restarted )).Entries );
		Assert.Equal( 4, store.Files.Files.Count );
	}

	[Fact]
	public async Task CorruptCurrentJournalIsPreservedAndBlocksFurtherWrites()
	{
		var store = new AuditStoreHarness();
		var service = Service( store );
		await service.RecordAsync( Action(), CommandStatus.Success, None );
		var file = store.Files.Files.Keys.Single( key => key.Contains( "/g-" ) );
		store.Files.Files[ file ] = "broken";
		Assert.False( await service.BeginAsync( Action(), None ) );
		Assert.Equal( StoreReadStatus.Corrupt, service.Diagnostics().ReadStatus );
		Assert.Equal( CommandStatus.StorageUnavailable, (await Query( service )).Status );
		Assert.Equal( "broken", store.Files.Files[ file ] );
	}

	[Fact]
	public async Task CleanupFailurePreservesCurrentAndCanResumeAfterMarkerDeletion()
	{
		var store = new AuditStoreHarness();
		var service = Service( store );
		await service.RecordAsync( Action(), CommandStatus.Success, None );
		await service.RecordAsync( Action(), CommandStatus.Success, None );
		store.BeforeDelete = path => { if ( path.Contains( "/g-" ) ) throw new IOException(); };
		Assert.False( await service.BeginAsync( Action(), None ) );
		Assert.True( service.Diagnostics().IsDegraded );
		Assert.Equal( CommandStatus.Success, (await Query( service )).Status );
		store.BeforeDelete = null;
		Assert.True( await service.MaintainAutomaticAsync( None ) );
		Assert.Equal( 4, store.Files.Files.Count );
		Assert.True( await service.BeginAsync( Action(), None ) );
	}
}
