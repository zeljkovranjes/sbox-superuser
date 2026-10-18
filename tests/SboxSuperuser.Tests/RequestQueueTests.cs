using SboxSuperuser.Core.Commands;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class RequestQueueTests
{
	private static RequestKey Key( string actor = "actor", int origin = 1, Guid? id = null ) => new( 1, actor, origin, id ?? Guid.NewGuid() );
	private static TaskCompletionSource<int> Barrier() => new( TaskCreationOptions.RunContinuationsAsynchronously );

	[Fact]
	public async Task RetriesShareOneExecutionAndChangedPayloadConflicts()
	{
		var barrier = Barrier();
		var queue = new RequestQueue<int>( () => DateTimeOffset.UtcNow, -1 );
		var key = Key();
		var count = 0;
		var first = queue.Submit( key, "payload", () => { count++; return barrier.Task; }, CancellationToken.None );
		var retry = queue.Submit( key, "payload", () => Task.FromResult( 999 ), CancellationToken.None );
		Assert.Equal( QueueAdmission.Duplicate, retry.Admission );
		Assert.Same( first.Result, retry.Result );
		Assert.Equal( QueueAdmission.Conflict, queue.Submit( key, "changed", () => Task.FromResult( 999 ), CancellationToken.None ).Admission );
		barrier.SetResult( 42 );
		Assert.Equal( 42, await first.Result );
		Assert.Equal( 42, await retry.Result );
		Assert.Equal( 1, count );
	}

	[Fact]
	public async Task OriginAndActorScopeRequestIdentifiers()
	{
		var queue = new RequestQueue<int>( () => DateTimeOffset.UtcNow, -1 );
		var id = Guid.NewGuid();
		var count = 0;
		foreach ( var key in new[] { Key( "one", 1, id ), Key( "one", 2, id ), Key( "two", 1, id ) } )
		{
			Assert.Equal( ++count, await queue.Submit( key, "same", () => Task.FromResult( count ), CancellationToken.None ).Result );
		}
	}

	[Fact]
	public async Task WorkIsSerializedAndActorQueueCannotExceedItsLimit()
	{
		var queue = new RequestQueue<int>( () => DateTimeOffset.UtcNow, -1 );
		var barrier = Barrier();
		var active = queue.Submit( Key(), "first", () => barrier.Task, CancellationToken.None );
		var executed = 0;
		var waiting = new List<Task<int>>();
		for ( var index = 1; index < 8; index++ )
		{
			waiting.Add( queue.Submit( Key(), "next", () => Task.FromResult( ++executed ), CancellationToken.None ).Result );
		}

		Assert.Equal( QueueAdmission.Busy, queue.Submit( Key(), "full", () => Task.FromResult( 999 ), CancellationToken.None ).Admission );
		Assert.Equal( 0, executed );
		barrier.SetResult( 0 );
		await active.Result;
		Assert.Equal( Enumerable.Range( 1, 7 ), await Task.WhenAll( waiting ) );
	}

	[Fact]
	public async Task GlobalQueueLimitAppliesAcrossActors()
	{
		var queue = new RequestQueue<int>( () => DateTimeOffset.UtcNow, -1 );
		var barrier = Barrier();
		var tasks = new List<Task<int>> { queue.Submit( Key(), "first", () => barrier.Task, CancellationToken.None ).Result };
		for ( var index = 1; index < 64; index++ )
		{
			tasks.Add( queue.Submit( Key( "actor" + index ), "next", () => Task.FromResult( 1 ), CancellationToken.None ).Result );
		}

		Assert.Equal( QueueAdmission.Busy, queue.Submit( Key( "overflow" ), "full", () => Task.FromResult( 999 ), CancellationToken.None ).Admission );
		barrier.SetResult( 0 );
		await Task.WhenAll( tasks );
	}

	[Fact]
	public async Task QueuedCancellationReturnsPromptlyWithoutInvokingOrFreeingUnboundedSlots()
	{
		var queue = new RequestQueue<int>( () => DateTimeOffset.UtcNow, -1 );
		var barrier = Barrier();
		var active = queue.Submit( Key(), "first", () => barrier.Task, CancellationToken.None );
		using var cancellation = new CancellationTokenSource();
		var invoked = false;
		var cancelled = queue.Submit( Key(), "cancel", () => { invoked = true; return Task.FromResult( 999 ); }, cancellation.Token );
		cancellation.Cancel();
		Assert.Equal( -1, await cancelled.Result.WaitAsync( TimeSpan.FromSeconds( 2 ) ) );
		Assert.False( active.Result.IsCompleted );
		for ( var index = 2; index < 8; index++ )
		{
			Assert.Equal( QueueAdmission.Accepted, queue.Submit( Key(), "cancel", () => Task.FromResult( 999 ), cancellation.Token ).Admission );
		}

		Assert.Equal( QueueAdmission.Busy, queue.Submit( Key(), "still full", () => Task.FromResult( 999 ), cancellation.Token ).Admission );
		barrier.SetResult( 0 );
		await active.Result;
		Assert.False( invoked );
	}

	[Fact]
	public async Task ActiveCancellationDoesNotInventARollbackResult()
	{
		var queue = new RequestQueue<int>( () => DateTimeOffset.UtcNow, -1 );
		var barrier = Barrier();
		using var cancellation = new CancellationTokenSource();
		var active = queue.Submit( Key(), "active", () => barrier.Task, cancellation.Token );
		cancellation.Cancel();
		Assert.False( active.Result.IsCompleted );
		barrier.SetResult( 42 );
		Assert.Equal( 42, await active.Result );
	}

	[Fact]
	public async Task PendingRetryNeverExpiresButCompletedRetryExpiresAfterFiveMinutes()
	{
		var now = DateTimeOffset.UtcNow;
		var queue = new RequestQueue<int>( () => now, -1 );
		var barrier = Barrier();
		var key = Key();
		var active = queue.Submit( key, "one", () => barrier.Task, CancellationToken.None );
		now += TimeSpan.FromDays( 1 );
		Assert.Equal( QueueAdmission.Duplicate, queue.Submit( key, "one", () => Task.FromResult( 999 ), CancellationToken.None ).Admission );
		barrier.SetResult( 1 );
		await active.Result;
		// A following queued item establishes that the first completion bookkeeping has finished.
		await queue.Submit( Key(), "barrier", () => Task.FromResult( 0 ), CancellationToken.None ).Result;
		now += TimeSpan.FromMinutes( 5 );
		var next = queue.Submit( key, "one", () => Task.FromResult( 2 ), CancellationToken.None );
		Assert.Equal( QueueAdmission.Accepted, next.Admission );
		Assert.Equal( 2, await next.Result );
	}

	[Fact]
	public async Task UnexpiredDedupEntriesAreNeverEvictedToAdmitNewWork()
	{
		var now = DateTimeOffset.UtcNow;
		var queue = new RequestQueue<int>( () => now, -1 );
		var first = Key();
		await queue.Submit( first, "one", () => Task.FromResult( 1 ), CancellationToken.None ).Result;
		for ( var index = 1; index < 128; index++ )
		{
			await queue.Submit( Key(), "one", () => Task.FromResult( 1 ), CancellationToken.None ).Result;
		}

		Assert.Equal( QueueAdmission.Busy, queue.Submit( Key(), "full", () => Task.FromResult( 999 ), CancellationToken.None ).Admission );
		Assert.Equal( QueueAdmission.Duplicate, queue.Submit( first, "one", () => Task.FromResult( 999 ), CancellationToken.None ).Admission );
	}

	[Fact]
	public async Task HandlerFailureIsObservableAndDoesNotStopFollowingWork()
	{
		var queue = new RequestQueue<int>( () => DateTimeOffset.UtcNow, -1 );
		var failed = queue.Submit( Key(), "fail", () => throw new InvalidOperationException( "test" ), CancellationToken.None );
		await Assert.ThrowsAsync<InvalidOperationException>( () => failed.Result );
		Assert.Equal( 2, await queue.Submit( Key(), "next", () => Task.FromResult( 2 ), CancellationToken.None ).Result );
	}
}
