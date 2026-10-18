namespace SboxSuperuser.Engine.Modules;

internal sealed class ModuleExecution
{
	private readonly CancellationTokenSource _cancellation = new();
	private TaskCompletionSource<bool> _idle;
	private int _active;
	internal CancellationToken Cancellation => _cancellation.Token;
	internal Task WhenIdle => _active == 0 ? Task.CompletedTask : _idle.Task;

	internal async Task<CommandResult> RunAsync( Func<Task<CommandResult>> execute )
	{
		if ( Cancellation.IsCancellationRequested )
		{
			return new CommandResult( CommandStatus.Cancelled, "Module entry points have stopped." );
		}
		if ( _active++ == 0 )
		{
			_idle = new TaskCompletionSource<bool>();
		}
		try
		{
			return await execute();
		}
		finally
		{
			if ( --_active == 0 )
			{
				_idle.TrySetResult( true );
			}
		}
	}

	internal void Stop() => _cancellation.Cancel();
}
