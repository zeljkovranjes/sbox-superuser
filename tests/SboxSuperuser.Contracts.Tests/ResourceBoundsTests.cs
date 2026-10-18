using SboxSuperuser.Engine.Audit;
using SboxSuperuser.Engine.Authority;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ResourceBoundsTests
{
	[Fact]
	public async Task CachedPermissionChecksDoNotReadStorageAndExplicitReloadFailsClosed()
	{
		var store = new MemoryPolicyStore();
		var errors = new List<Exception>();
		var policy = new HostPolicyService( store, Array.Empty<string>(), StoreReadStatus.Missing, () => true, errors.Add );
		await policy.Initialization;
		store.ThrowOnRead = true;
		for ( var index = 0; index < 10000; index++ )
		{
			Assert.True( policy.Explain( DispatcherRig.User, "admin.help" ).IsAllowed );
			Assert.False( policy.Explain( DispatcherRig.User, "admin.players.ban" ).IsAllowed );
		}
		Assert.Empty( errors );
		Assert.Equal( CommandStatus.StorageUnavailable, (await policy.ReloadAsync( CancellationToken.None )).Status );
		Assert.False( policy.Explain( DispatcherRig.User, "admin.help" ).IsAllowed );
		Assert.NotEmpty( errors );
	}

	[Fact]
	public async Task AuditPressureRejectsExcessWithoutCreatingUnboundedWaiters()
	{
		var release = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
		var store = new AuditStoreHarness();
		var errors = new List<Exception>();
		var service = new HostAuditService( store, () => true, errors.Add );
		await service.Initialization;
		store.BeforeWrite = async _ =>
		{
			await release.Task;
			return null;
		};
		var calls = Enumerable.Range( 0, 1000 ).Select( _ => service.RecordAsync(
			new AuditAction( DispatcherRig.User, CommandOrigin.Console, Guid.NewGuid(), "pressure.test", DateTimeOffset.UtcNow ),
			CommandStatus.Denied, CancellationToken.None ) ).ToArray();
		try
		{
			Assert.Equal( 64, calls.Count( call => !call.IsCompleted ) );
			Assert.Equal( 936, calls.Count( call => call.IsCompletedSuccessfully && !call.Result ) );
		}
		finally
		{
			release.TrySetResult( true );
		}
		var results = await Task.WhenAll( calls ).WaitAsync( TimeSpan.FromSeconds( 10 ) );
		Assert.Equal( 64, results.Count( result => result ) );
		Assert.Equal( 64, service.Diagnostics().RetainedCount );
		Assert.Empty( errors );
	}
}
