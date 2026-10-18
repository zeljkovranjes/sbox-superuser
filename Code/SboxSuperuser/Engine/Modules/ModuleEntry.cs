namespace SboxSuperuser.Engine.Modules;

internal sealed class ModuleEntry
{
	internal IAdminModule Module { get; }
	internal ModuleDescriptor Descriptor { get; }
	internal ModuleState State { get; set; }
	internal string Message { get; set; } = "Installed; disabled.";
	internal CommandRegistrationScope Scope { get; set; }
	internal CancellationTokenSource Cancellation { get; set; }
	internal Task Startup { get; set; }
	internal Task<bool> Cleanup { get; set; }
	internal ModuleState AfterCleanup { get; set; }
	internal bool CleanupFailed { get; set; }

	internal ModuleEntry( IAdminModule module, ModuleDescriptor descriptor )
	{
		Module = module;
		Descriptor = descriptor;
		if ( module is null )
		{
			State = ModuleState.Enabled;
			Message = "Required framework security service.";
		}
	}
}
