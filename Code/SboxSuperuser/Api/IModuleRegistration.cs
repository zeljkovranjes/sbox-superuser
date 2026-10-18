namespace SboxSuperuser;

/// <summary>A host-owned registration scope tied to one module; declarations do not execute commands.</summary>
public interface IModuleRegistration
{
	/// <summary>Gets the owning module; registrations cannot impersonate another module.</summary>
	string ModuleId { get; }

	/// <summary>Declares a command and handler; implementations must validate ownership and collisions.</summary>
	void RegisterCommand( CommandDescriptor descriptor, CommandHandler handler );

	/// <summary>Declares a permission owned by this module; no grant is performed.</summary>
	void RegisterPermission( PermissionDescriptor descriptor );

	/// <summary>Tracks a listener or resource for reverse-order disposal on rollback or shutdown.</summary>
	void Track( IDisposable subscription );

	/// <summary>Approves an exact game capability type for this module's handlers.</summary>
	void RegisterService<T>( T service ) where T : class;

	/// <summary>Stages attributed commands through the verified engine discovery adapter.</summary>
	void DiscoverCommands();
}
