using SboxSuperuser.Core.Audit;
using SboxSuperuser.Core.Storage;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Audit;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class SecurityRegressionTests
{
	[Fact]
	public async Task MaximumAdminActivityPageReadsOneSnapshot()
	{
		using var rig = new ModerationRig();
		var store = new CountingAuditStore();
		var audit = new HostAuditService( store, () => true, _ => { } );
		await audit.Initialization;
		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, audit, () => rig.Rig.Players );
		store.Reads = 0;

		var reply = await queries.QueryAsync( () => DispatcherRig.Owner,
			new AdminQuery( "", "", null, 64 ), Guid.NewGuid(), CancellationToken.None );

		Assert.Equal( CommandStatus.Success, reply.Status );
		Assert.Empty( reply.Activity );
		Assert.Equal( 1, store.Reads );
	}

	[Fact]
	public async Task OffsetAuditPageReturnsCorrectRows()
	{
		var store = new CountingAuditStore();
		var service = new HostAuditService( store, () => true, _ => { } );
		await service.Initialization;
		for ( var index = 0; index < 21; index++ )
		{
			var action = new AuditAction( DispatcherRig.Owner, CommandOrigin.Interface, Guid.NewGuid(), "test.run",
				DateTimeOffset.UtcNow.AddSeconds( index ) );
			Assert.True( await service.RecordAsync( action, CommandStatus.Success, CancellationToken.None ) );
		}

		store.Reads = 0;
		var page = await service.QueryPageAsync( () => DispatcherRig.Owner, _ => true, _ => false,
			new AuditQuery( pageSize: 20 ), 1, CancellationToken.None );

		Assert.Equal( CommandStatus.Success, page.Status );
		Assert.Single( page.Entries );
		Assert.Equal( 1, store.Reads );
	}

	private sealed class CountingAuditStore : IAdminStore, IAuditRetention
	{
		private readonly AuditStoreHarness _inner = new();
		internal int Reads;

		public Task<StoreReadResult> ReadAsync( StoreKey key, int schema, CancellationToken cancellation )
		{
			Reads++;
			return _inner.ReadAsync( key, schema, cancellation );
		}

		public Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation ) =>
			_inner.WriteAsync( request, cancellation );

		public Task<StoragePruneResult> PruneAuditAsync( long revision, CancellationToken cancellation ) =>
			_inner.PruneAuditAsync( revision, cancellation );
	}
}
