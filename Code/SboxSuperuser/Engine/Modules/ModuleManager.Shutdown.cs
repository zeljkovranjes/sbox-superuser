namespace SboxSuperuser.Engine.Modules;

internal sealed partial class ModuleManager
{
	private async Task<CommandResult> DisableAsync( ModuleEntry entry, CancellationToken cancellation )
	{
		Refresh( entry );
		if ( entry.Descriptor.IsRequiredSecurityModule )
		{
			return Result( CommandStatus.Denied, "Required security modules cannot be disabled." );
		}
		if ( cancellation.IsCancellationRequested )
		{
			return Result( CommandStatus.Cancelled, "Module disable was cancelled before changing entry points." );
		}
		foreach ( var dependent in _entries.Values )
		{
			Refresh( dependent );
			if ( dependent.State is not (ModuleState.Disabled or ModuleState.Faulted)
				&& dependent.Descriptor.Dependencies.Any( dependency => dependency.ModuleId == entry.Descriptor.Id && !dependency.IsOptional ) )
			{
				return Result( CommandStatus.Conflict, $"Disable dependent module '{dependent.Descriptor.Id}' first." );
			}
		}
		if ( entry.State == ModuleState.Disabled || entry.Scope is null )
		{
			return Result( CommandStatus.Success, "Module is already disabled." );
		}
		await StopEntryAsync( entry, ModuleState.Disabled, cancellation );
		return entry.State == ModuleState.Disabled ? Result( CommandStatus.Success, "Module disabled; stored data retained." )
			: Result( CommandStatus.Unknown, "Entry points removed; cleanup is quarantined. No rollback is implied." );
	}

	private async Task StopEntryAsync( ModuleEntry entry, ModuleState afterCleanup, CancellationToken cancellation )
	{
		BeginStop( entry, afterCleanup );
		await WithinDeadlineAsync( entry.Cleanup, cancellation );
		Refresh( entry );
		if ( !entry.Cleanup.IsCompleted )
		{
			entry.State = ModuleState.Quarantined;
			entry.Message = "Shutdown exceeded its deadline; waiting for the previous generation.";
		}
	}

	private void BeginStop( ModuleEntry entry, ModuleState afterCleanup )
	{
		if ( entry.Cleanup is not null )
		{
			return;
		}
		entry.State = ModuleState.Stopping;
		entry.AfterCleanup = afterCleanup;
		entry.Message = "Entry points removed; cleanup in progress.";
		try
		{
			entry.Scope?.Dispose();
		}
		catch ( Exception error )
		{
			entry.CleanupFailed = true;
			_report( error );
		}
		Cancel( entry );
		entry.Cleanup = CleanupAsync( entry );
	}

	private void Cancel( ModuleEntry entry )
	{
		try
		{
			entry.Cancellation?.Cancel();
		}
		catch ( Exception error )
		{
			entry.CleanupFailed = true;
			_report( error );
		}
	}

	private async Task<bool> CleanupAsync( ModuleEntry entry )
	{
		try
		{
			await (entry.Startup ?? Task.CompletedTask);
		}
		catch ( Exception error )
		{
			_report( error );
		}
		await (entry.Scope?.Execution?.WhenIdle ?? Task.CompletedTask);
		try
		{
			using var stop = new CancellationTokenSource( _deadline );
			await entry.Module.StopAsync( stop.Token );
		}
		catch ( Exception error )
		{
			entry.CleanupFailed = true;
			_report( error );
		}
		return !entry.CleanupFailed;
	}

	private static void Refresh( ModuleEntry entry )
	{
		if ( entry.Cleanup is not { IsCompleted: true } )
		{
			return;
		}
		entry.State = entry.Cleanup.IsCompletedSuccessfully && entry.Cleanup.Result ? entry.AfterCleanup : ModuleState.Quarantined;
		entry.Message = entry.State switch
		{
			ModuleState.Disabled => "Disabled; stored data retained.",
			ModuleState.Faulted => "Startup failed; contributions removed.",
			_ => "Cleanup failed; repair the module and restart the session."
		};
	}

	public void Dispose()
	{
		if ( _disposed )
		{
			return;
		}
		_disposed = true;
		foreach ( var id in _order.Reverse() )
		{
			var entry = _entries[ id ];
			if ( entry.Module is not null && entry.Scope is not null )
			{
				BeginStop( entry, ModuleState.Disabled );
			}
		}
	}
}
