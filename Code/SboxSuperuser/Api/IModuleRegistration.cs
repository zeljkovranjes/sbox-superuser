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
}
