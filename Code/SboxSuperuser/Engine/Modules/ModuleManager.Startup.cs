namespace SboxSuperuser.Engine.Modules;

internal sealed partial class ModuleManager
{
	private async Task<CommandResult> EnableAsync( ModuleEntry entry, Func<bool> authorized, CancellationToken cancellation )
	{
		Refresh( entry );
		if ( cancellation.IsCancellationRequested || _disposed || !_available() || !authorized() )
		{
			return Result( CommandStatus.Cancelled, "Module startup authority or lifetime ended." );
		}
		if ( entry.State == ModuleState.Enabled )
		{
			return Result( CommandStatus.Success, "Module is already enabled." );
		}
		if ( entry.State is ModuleState.Quarantined or ModuleState.Starting or ModuleState.Stopping )
		{
			return Result( CommandStatus.Conflict, "The previous module generation has not completed cleanup." );
		}
		entry.Scope = new CommandRegistrationScope( _registry, entry.Descriptor.Id, _discover, managed: true );
		entry.Cancellation = new CancellationTokenSource();
		entry.Cleanup = null;
		entry.CleanupFailed = false;
		entry.State = ModuleState.Starting;
		entry.Message = "Staging contributions.";
		using var cancelled = cancellation.Register( () => Cancel( entry ) );
		try
		{
			entry.Startup = entry.Module.StartAsync( new ModuleRegistration( entry.Scope ), entry.Cancellation.Token )
				?? Task.FromException( new InvalidOperationException( "Module startup returned no task." ) );
		}
		catch ( Exception error )
		{
			entry.Startup = Task.FromException( error );
		}

		var finished = await WithinDeadlineAsync( entry.Startup, cancellation );
		if ( finished && entry.Startup.IsCompletedSuccessfully && !_disposed && _available() && authorized()
			&& !cancellation.IsCancellationRequested && !entry.Cancellation.IsCancellationRequested )
		{
			try
			{
				entry.Scope.Commit();
				entry.State = ModuleState.Enabled;
				entry.Message = "Enabled.";
				return Result( CommandStatus.Success, "Module enabled." );
			}
			catch ( Exception error )
			{
				_report( error );
			}
		}

		await StopEntryAsync( entry, ModuleState.Faulted, cancellation );
		return Result( entry.State == ModuleState.Quarantined ? CommandStatus.Unknown : CommandStatus.Failed,
			$"Module '{entry.Descriptor.Id}' did not start; registrations were rolled back. {entry.Message}" );
	}

	private async Task<bool> WithinDeadlineAsync( Task work, CancellationToken cancellation )
	{
		// Task.WhenAny overloads are blocked by the engine whitelist. Yield in bounded timer slices.
		var timer = System.Diagnostics.Stopwatch.StartNew();
		while ( !work.IsCompleted && !cancellation.IsCancellationRequested && timer.Elapsed < _deadline )
		{
			await Task.Delay( 10 );
		}
		return work.IsCompleted;
	}
}
