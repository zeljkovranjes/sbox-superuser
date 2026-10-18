namespace SboxSuperuser;

/// <summary>In-process module lifecycle contract; no loader or module implementation is supplied.</summary>
public interface IAdminModule
{
	/// <summary>Gets stable ownership and compatibility metadata.</summary>
	ModuleDescriptor Descriptor { get; }

	/// <summary>Registers module contributions with the host-owned scope; exceptions mean startup failed.</summary>
	Task StartAsync( IModuleRegistration registration, CancellationToken cancellation );

	/// <summary>Stops work and releases subscriptions after the manager stops ingress; preserves stored data.</summary>
	Task StopAsync( CancellationToken cancellation );
}
