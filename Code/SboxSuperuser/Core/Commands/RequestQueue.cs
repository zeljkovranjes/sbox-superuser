namespace SboxSuperuser.Core.Commands;

internal sealed class RequestQueue<T>
{
	internal const int MaximumOutstanding = 64;
	internal const int MaximumActorOutstanding = 8;
	internal const int MaximumRemembered = 1024;
	internal const int MaximumActorRemembered = 128;
	private static readonly TimeSpan RetryLifetime = TimeSpan.FromMinutes( 5 );
	private readonly object _gate = new();
	private readonly Queue<RequestWork<T>> _pending = new();
	private readonly Dictionary<RequestKey, RequestWork<T>> _remembered = new();
	private readonly Func<DateTimeOffset> _clock;
	private readonly T _cancelled;
	private bool _draining;
	private int _outstanding;

	internal RequestQueue( Func<DateTimeOffset> clock, T cancelled )
	{
		_clock = clock;
		_cancelled = cancelled;
	}

	internal QueuedResult<T> Submit( RequestKey key, string fingerprint, Func<Task<T>> execute, CancellationToken cancellation )
	{
		RequestWork<T> work;
		bool start;
		lock ( _gate )
		{
			Prune();
			if ( _remembered.TryGetValue( key, out var existing ) )
			{
				return new QueuedResult<T>( existing.Fingerprint == fingerprint ? QueueAdmission.Duplicate : QueueAdmission.Conflict,
					existing.Fingerprint == fingerprint ? existing.Completion.Task : null );
			}

			var actorEntries = _remembered.Values.Where( value => value.Key.ActorKind == key.ActorKind && value.Key.SubjectId == key.SubjectId ).ToArray();
			if ( _outstanding >= MaximumOutstanding || _remembered.Count >= MaximumRemembered || actorEntries.Length >= MaximumActorRemembered
				|| actorEntries.Count( value => !value.CompletedUtc.HasValue ) >= MaximumActorOutstanding )
			{
				return new QueuedResult<T>( QueueAdmission.Busy, null );
			}

			work = new RequestWork<T>( key, fingerprint, execute );
			_remembered.Add( key, work );
			_pending.Enqueue( work );
			_outstanding++;
			work.Cancellation = cancellation.Register( () => CancelQueued( work ) );
			start = !_draining;
			_draining = true;
		}

		if ( start )
		{
			_ = DrainAsync();
		}

		return new QueuedResult<T>( QueueAdmission.Accepted, work.Completion.Task );
	}

	private async Task DrainAsync()
	{
		while ( true )
		{
			RequestWork<T> work;
			lock ( _gate )
			{
				if ( _pending.Count == 0 )
				{
					_draining = false;
					return;
				}

				work = _pending.Dequeue();
				work.Started = true;
			}

			try
			{
				if ( !work.Completion.Task.IsCompleted )
				{
					work.Completion.TrySetResult( await work.Execute() );
				}
			}
			catch ( Exception error )
			{
				// The host adapter observes and reports this failure; later queued work must still drain.
				work.Completion.TrySetException( error );
			}
			finally
			{
				work.Cancellation.Dispose();
				lock ( _gate )
				{
					work.CompletedUtc = _clock();
					_outstanding--;
				}
			}
		}
	}

	private void CancelQueued( RequestWork<T> work )
	{
		lock ( _gate )
		{
			if ( !work.Started )
			{
				work.Completion.TrySetResult( _cancelled );
			}
		}
	}

	private void Prune()
	{
		var now = _clock();
		foreach ( var key in _remembered.Where( pair => pair.Value.CompletedUtc.HasValue && now - pair.Value.CompletedUtc.Value >= RetryLifetime )
			.Select( pair => pair.Key ).ToArray() )
		{
			_remembered.Remove( key );
		}
	}
}
