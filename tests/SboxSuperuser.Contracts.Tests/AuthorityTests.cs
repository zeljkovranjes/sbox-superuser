using SboxSuperuser.Core.Permissions;
using SboxSuperuser.Engine.Authority;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AuthorityTests
{
	private const string Owner = "76561198000000001";
	private const string Other = "76561198000000002";
	private const string Offline = "76561198000000003";
	private static readonly ActorIdentity OwnerActor = new( ActorKind.Player, Owner );
	private static readonly ActorIdentity OtherActor = new( ActorKind.Player, Other );
	private static readonly ActorIdentity LocalConsole = new( ActorKind.DedicatedConsole, "dedicated-console" );
	private static readonly CancellationToken None = CancellationToken.None;

	private static HostPolicyService Service( MemoryPolicyStore store, IEnumerable<string> owners = null, Func<bool> available = null )
		=> new( store, owners ?? new[] { Owner }, StoreReadStatus.Found, available ?? (() => true), _ => { } );

	private static Task<CommandResult> Apply( HostPolicyService service, ActorIdentity actor, PolicyChange change )
		=> service.ApplyAsync( () => actor, change, CommandOrigin.Console, Guid.NewGuid(), None );

	private static string Payload( string state ) => new PolicyTransaction( state, new[]
	{
		new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), ActorKind.DedicatedConsole, "dedicated-console",
			CommandOrigin.Console, "test.policy", DateTimeOffset.UtcNow )
	} ).CreateWriteRequest( HostPolicyService.PolicyKey, 0 ).Document.PayloadJson;

	[Theory]
	[InlineData( 10, 0, true )]
	[InlineData( 10, 10, false )]
	[InlineData( 10, 50, false )]
	[InlineData( 50, 10, true )]
	[InlineData( 50, 50, false )]
	[InlineData( 0, 0, false )]
	public void PunitiveProtectionUsesStrictOfflineRankComparison( int actorLevel, int targetLevel, bool expected )
	{
		var policy = new PolicySnapshot( 1, new[]
		{
			new GroupPolicy( "player" ), new GroupPolicy( "actor", protectionLevel: actorLevel ),
			new GroupPolicy( "target", protectionLevel: targetLevel )
		}, new[] { new UserPolicy( Owner, new[] { "actor" } ), new UserPolicy( Offline, new[] { "target" } ) } );
		var result = TargetProtection.Check( policy.Graph, new OwnerSet( Array.Empty<string>() ), Owner, false, Offline );
		Assert.Equal( expected, result.IsAllowed );
		Assert.Equal( actorLevel, result.ActorLevel );
		Assert.Equal( targetLevel, result.TargetLevel );
	}

	[Fact]
	public void ProtectionUsesHighestInheritedLevelAndRefusesSelfEvenForOwner()
	{
		var defaults = PolicySnapshot.CreateDefault();
		var graph = new PolicySnapshot( 1, defaults.Groups, new[] { new UserPolicy( Other, new[] { "admin", "moderator" } ) } ).Graph;
		var owners = new OwnerSet( new[] { Owner } );
		Assert.Equal( 50, TargetProtection.Level( graph, owners, Other ) );
		Assert.Equal( 100, TargetProtection.Level( graph, owners, Owner ) );
		Assert.Equal( 0, TargetProtection.Level( graph, owners, Offline ) );
		Assert.False( TargetProtection.Check( graph, owners, Owner, false, Owner ).IsAllowed );
		Assert.False( TargetProtection.Check( graph, owners, Other, false, Owner ).IsAllowed );
		Assert.True( TargetProtection.Check( graph, owners, "dedicated-console", true, Owner ).IsAllowed );
	}

	[Fact]
	public async Task OrdinaryAdminCannotEditPolicyEvenWithExplicitSecurityPermission()
	{
		var store = new MemoryPolicyStore();
		var service = Service( store );
		Assert.Equal( CommandStatus.Success, (await Apply( service, OwnerActor, PolicyChange.AddUserGroup( Other, "admin" ) )).Status );
		Assert.Equal( CommandStatus.Success, (await Apply( service, OwnerActor, PolicyChange.SetUserPermission( Other, "admin.security.manage", PermissionEffect.Allow ) )).Status );
		Assert.True( service.Explain( OtherActor, "admin.security.manage" ).IsAllowed );
		Assert.Equal( CommandStatus.Denied, (await Apply( service, OtherActor, PolicyChange.CreateGroup( "escalation" ) )).Status );
		Assert.Equal( CommandStatus.Denied, (await Apply( service, null, PolicyChange.CreateGroup( "nullroot" ) )).Status );
		Assert.Equal( CommandStatus.Denied, (await Apply( service, new ActorIdentity( ActorKind.InternalService, "test.service" ), PolicyChange.CreateGroup( "servicegroup" ) )).Status );
		Assert.Equal( 2, store.WriteCount );
		Assert.False( store.Documents.ContainsKey( "security/owners" ) );
	}

	[Fact]
	public async Task OwnExplainWorksButAnotherSubjectRequiresItsSeparatePermission()
	{
		var service = Service( new MemoryPolicyStore() );
		Assert.NotNull( await service.ExplainForAsync( () => OtherActor, Other, "admin.help" ) );
		Assert.Null( await service.ExplainForAsync( () => OtherActor, Owner, "admin.help" ) );
		var owner = await service.ExplainForAsync( () => OwnerActor, Owner, "admin.players.ban" );
		Assert.True( owner.IsAllowed );
		Assert.True( owner.OwnerBypass );
		Assert.False( service.Explain( OwnerActor, "unknown.permission" ).IsAllowed );
		Assert.False( service.Explain( OwnerActor, "unknown.permission" ).OwnerBypass );
		await Apply( service, OwnerActor, PolicyChange.SetUserPermission( Other, "admin.permissions.explain", PermissionEffect.Allow ) );
		Assert.NotNull( await service.ExplainForAsync( () => OtherActor, Owner, "admin.help" ) );
	}

	[Fact]
	public async Task PolicyActivationAndSuccessWaitForPersistence()
	{
		var store = new MemoryPolicyStore();
		var started = new TaskCompletionSource();
		var resume = new TaskCompletionSource();
		store.BeforeCommit = async (_, _) => { started.TrySetResult(); await resume.Task; };
		var service = Service( store );
		var operation = Apply( service, OwnerActor, PolicyChange.SetUserPermission( Other, "admin.players.kick", PermissionEffect.Allow ) );
		await started.Task;
		Assert.False( operation.IsCompleted );
		Assert.Equal( 0, service.PolicyRevision );
		Assert.False( service.Explain( OtherActor, "admin.players.kick" ).IsAllowed );
		resume.SetResult();
		Assert.Equal( CommandStatus.Success, (await operation).Status );
		Assert.Equal( 1, service.PolicyRevision );
		Assert.True( service.Explain( OtherActor, "admin.players.kick" ).IsAllowed );
		Assert.Contains( "Unknown", store.Documents[ "security/policy" ].PayloadJson );
		Assert.Contains( "security.policy.change", store.Documents[ "security/policy" ].PayloadJson );
	}

	[Fact]
	public async Task FailedPersistenceDoesNotPublishCandidatePolicy()
	{
		var store = new MemoryPolicyStore { WriteOverride = StoreWriteStatus.Unavailable };
		var service = Service( store );
		var result = await Apply( service, OwnerActor, PolicyChange.SetUserPermission( Other, "admin.players.kick", PermissionEffect.Allow ) );
		Assert.Equal( CommandStatus.StorageUnavailable, result.Status );
		Assert.Equal( StoreWriteStatus.Unavailable, service.LastWriteStatus );
		Assert.Equal( 0, service.PolicyRevision );
		Assert.False( service.Explain( OtherActor, "admin.players.kick" ).IsAllowed );
		Assert.Empty( store.Documents );
	}

	[Fact]
	public async Task RevokedOwnerCannotExecuteAnAlreadyQueuedPolicyMutation()
	{
		var store = new MemoryPolicyStore();
		var started = new TaskCompletionSource();
		var resume = new TaskCompletionSource();
		store.BeforeCommit = async (_, _) => { started.TrySetResult(); await resume.Task; };
		var service = Service( store, new[] { Owner, Other } );
		var revoke = service.ChangeOwnerAsync( () => LocalConsole, Owner, false, Guid.NewGuid(), None );
		await started.Task;
		var queued = Apply( service, OwnerActor, PolicyChange.CreateGroup( "staleowner" ) );
		Assert.False( queued.IsCompleted );
		resume.SetResult();
		Assert.Equal( CommandStatus.Success, (await revoke).Status );
		Assert.Equal( CommandStatus.Denied, (await queued).Status );
		Assert.False( service.IsOwner( Owner ) );
		Assert.True( service.IsOwner( Other ) );
		Assert.Equal( 1, store.WriteCount );
	}

	[Fact]
	public async Task FinalOwnerCanOnlyBeClearedThroughExplicitLocalRecovery()
	{
		var store = new MemoryPolicyStore();
		var service = Service( store );
		Assert.Equal( CommandStatus.Denied, (await service.ChangeOwnerAsync( () => OwnerActor, Other, true, Guid.NewGuid(), None )).Status );
		Assert.Equal( CommandStatus.Denied, (await service.ChangeOwnerAsync( () => LocalConsole, Owner, false, Guid.NewGuid(), None )).Status );
		Assert.Empty( store.Documents );
		Assert.Equal( CommandStatus.Denied, (await service.RecoverOwnersAsync( () => OwnerActor, Array.Empty<string>(), Guid.NewGuid(), None )).Status );
		Assert.Equal( CommandStatus.Success, (await service.RecoverOwnersAsync( () => LocalConsole, Array.Empty<string>(), Guid.NewGuid(), None )).Status );
		Assert.False( service.IsOwner( Owner ) );
		var restarted = Service( store );
		await restarted.Initialization;
		Assert.False( restarted.IsOwner( Owner ) );
		Assert.Equal( "managed", restarted.OwnerSource );
		Assert.Equal( 0, restarted.OwnerCount );
		Assert.Equal( CommandStatus.Success, (await restarted.RecoverOwnersAsync( () => LocalConsole, new[] { Other }, Guid.NewGuid(), None )).Status );
		Assert.True( restarted.IsOwner( Other ) );
	}

	[Fact]
	public async Task CorruptManagedOwnersNeverFallBackToBootstrapAndCannotBeResetAsMissing()
	{
		var store = new MemoryPolicyStore();
		var service = Service( store );
		await service.ChangeOwnerAsync( () => LocalConsole, Other, true, Guid.NewGuid(), None );
		store.ReadOverrides[ "security/owners" ] = StoreReadStatus.Corrupt;
		await service.ReloadAsync( None );
		Assert.False( service.IsOwner( Owner ) );
		Assert.False( service.IsOwner( Other ) );
		Assert.Equal( StoreReadStatus.Corrupt, service.OwnerStatus );
		Assert.Equal( CommandStatus.StorageUnavailable, (await service.RecoverOwnersAsync( () => LocalConsole, new[] { Owner }, Guid.NewGuid(), None )).Status );
		store.ReadOverrides[ "security/owners" ] = StoreReadStatus.Missing;
		await service.ReloadAsync( None );
		Assert.Equal( StoreReadStatus.Corrupt, service.OwnerStatus );
		Assert.False( service.IsOwner( Owner ) );
		Assert.Equal( 1, store.WriteCount );
	}

	[Fact]
	public async Task InvalidPersistedPolicyAndReadExceptionsReportDegradedAuthority()
	{
		var store = new MemoryPolicyStore();
		store.Documents[ "security/policy" ] = new VersionedStoreDocument( 1, 1, Payload( "{}" ) );
		var service = Service( store );
		await service.Initialization;
		Assert.Equal( StoreReadStatus.Corrupt, service.PolicyStatus );
		Assert.False( service.Explain( OwnerActor, "admin.help" ).IsAllowed );
		Assert.Equal( CommandStatus.StorageUnavailable, (await Apply( service, LocalConsole, PolicyChange.CreateGroup( "reset" ) )).Status );
		Assert.Equal( 0, store.WriteCount );
		store.ThrowOnRead = true;
		var unavailable = Service( store );
		await unavailable.Initialization;
		Assert.Equal( StoreReadStatus.Unavailable, unavailable.PolicyStatus );
		Assert.False( unavailable.IsUsable );
	}

	[Fact]
	public async Task RecoveryNotificationInvalidatesAuthorizationAgainstStoredRevision()
	{
		var store = new MemoryPolicyStore();
		var service = Service( store );
		await Apply( service, OwnerActor, PolicyChange.SetUserPermission( Other, "admin.players.kick", PermissionEffect.Allow ) );
		Assert.True( service.Explain( OtherActor, "admin.players.kick" ).IsAllowed );
		var replacement = PolicySnapshot.CreateDefault( 2 );
		store.Documents[ "security/policy" ] = new VersionedStoreDocument( 1, 2, Payload( PolicyCodec.WriteState( replacement.Graph ) ) );
		service.OnStorageChanged( HostPolicyService.PolicyKey );
		await service.ReloadAsync( None );
		Assert.Equal( 2, service.PolicyRevision );
		Assert.False( service.Explain( OtherActor, "admin.players.kick" ).IsAllowed );
	}

	[Fact]
	public void PolicyEditsRejectCyclesAndReferencedDeletionWithoutChangingTheOriginal()
	{
		var graph = PolicySnapshot.CreateDefault().Graph;
		Assert.Throws<ArgumentException>( () => PolicyEdits.Apply( graph, PolicyChange.SetParents( "player", new[] { "admin" } ).Edit ) );
		Assert.Throws<ArgumentException>( () => PolicyEdits.Apply( graph, PolicyChange.DeleteGroup( "player" ).Edit ) );
		Assert.Throws<ArgumentException>( () => PolicyEdits.Apply( graph, PolicyChange.DeleteGroup( "moderator" ).Edit ) );
		Assert.Throws<ArgumentException>( () => PolicyChange.AddUserGroup( Owner, "owner" ) );
		Assert.Empty( graph.Users );
		Assert.Equal( 0, graph.Revision );
		var changed = PolicyEdits.Apply( graph, PolicyChange.AddUserGroup( Offline, "admin" ).Edit );
		Assert.Equal( 1, changed.Revision );
		Assert.Equal( "admin", Assert.Single( changed.Users[ Offline ].Groups ) );
		Assert.Same( changed, PolicyEdits.Apply( changed, PolicyChange.AddUserGroup( Offline, "admin" ).Edit ) );
		var roundTrip = PolicyCodec.ReadPayload( Payload( PolicyCodec.WriteState( changed ) ), 1 );
		Assert.Equal( PolicyCodec.WriteState( changed ), PolicyCodec.WriteState( roundTrip ) );
	}

	[Fact]
	public async Task RetiredRuntimeCannotExplainTargetOrMutate()
	{
		var available = true;
		var store = new MemoryPolicyStore();
		var service = Service( store, available: () => available );
		Assert.True( service.IsOwner( Owner ) );
		available = false;
		Assert.False( service.IsOwner( Owner ) );
		Assert.False( service.Explain( OwnerActor, "admin.help" ).IsAllowed );
		Assert.False( service.CheckTarget( LocalConsole, Offline ).IsAllowed );
		Assert.Equal( CommandStatus.Unsupported, (await Apply( service, LocalConsole, PolicyChange.CreateGroup( "retired" ) )).Status );
		Assert.Empty( store.Documents );
	}
}
