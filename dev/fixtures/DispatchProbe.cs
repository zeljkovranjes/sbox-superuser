using Sandbox;
using SboxSuperuser;
using System;
using System.Threading.Tasks;

namespace SuperuserCompatibilityLab.Engine.Commands;

internal sealed class DispatchProbe : IDispatchProbe
{
	private TaskCompletionSource<CommandResult> _hold;
	/// <summary>Gets observed game capability mutations.</summary>
	public int Count { get; private set; }

	/// <summary>Starts a test-controlled barrier; no blocking wait runs on the engine thread.</summary>
	public void Hold() => _hold = new( TaskCreationOptions.RunContinuationsAsynchronously );

	/// <summary>Releases the active handler.</summary>
	public void Release() => _hold?.TrySetResult( new CommandResult( CommandStatus.Success, "Released" ) );

	/// <summary>Returns the asynchronous test barrier.</summary>
	public Task<CommandResult> WaitAsync() => _hold?.Task ?? throw new InvalidOperationException( "No test barrier." );

	/// <summary>Exercises a compiled adopting-game capability on the main thread.</summary>
	public CommandResult Increment()
	{
		if ( !ThreadSafe.IsMainThread )
		{
			throw new InvalidOperationException( "Game capability resumed outside the main thread." );
		}

		Count++;
		return new CommandResult( CommandStatus.Success, "Counter incremented" );
	}
}
