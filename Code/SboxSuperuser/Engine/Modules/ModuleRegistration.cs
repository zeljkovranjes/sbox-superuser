namespace SboxSuperuser.Engine.Modules;

// Modules receive only staging operations; they cannot commit or replace another scope.
internal sealed class ModuleRegistration : IModuleRegistration
{
	private readonly CommandRegistrationScope _scope;
	internal ModuleRegistration( CommandRegistrationScope scope ) => _scope = scope;
	public string ModuleId => _scope.ModuleId;
	public void RegisterCommand( CommandDescriptor descriptor, CommandHandler handler ) => _scope.RegisterCommand( descriptor, handler );
	public void RegisterPermission( PermissionDescriptor descriptor ) => _scope.RegisterPermission( descriptor );
	public void RegisterService<T>( T service ) where T : class => _scope.RegisterService( service );
	public void DiscoverCommands() => _scope.DiscoverCommands();
	public void Track( IDisposable subscription ) => _scope.Track( subscription );
}
