namespace SboxSuperuser;

/// <summary>Trusted in-process module lifecycle; register through Superuser.RegisterModulesAsync on the host.</summary>
public interface IAdminModule
{
	/// <summary>Gets stable ownership and compatibility metadata.</summary>
	ModuleDescriptor Descriptor { get; }

	/// <summary>Stages contributions and tracked subscriptions; nothing commits until this task succeeds.</summary>
	Task StartAsync( IModuleRegistration registration, CancellationToken cancellation );

	/// <summary>Stops work and releases subscriptions after the manager stops ingress; preserves stored data.</summary>
	Task StopAsync( CancellationToken cancellation );
}
