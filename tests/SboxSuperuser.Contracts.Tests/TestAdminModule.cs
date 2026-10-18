namespace SboxSuperuser.Contracts.Tests;

internal sealed class TestAdminModule : IAdminModule
{
	public ModuleDescriptor Descriptor { get; }
	internal Func<IModuleRegistration, CancellationToken, Task> Start { get; set; } = ( _, _ ) => Task.CompletedTask;
	internal Func<CancellationToken, Task> Stop { get; set; } = _ => Task.CompletedTask;
	internal int Starts { get; private set; }
	internal int Stops { get; private set; }
	internal TestAdminModule( string id, bool required = false, params ModuleDependency[] dependencies )
		=> Descriptor = new ModuleDescriptor( id, "Test module", 1, dependencies, required );
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		Starts++;
		return Start( registration, cancellation );
	}
	public Task StopAsync( CancellationToken cancellation )
	{
		Stops++;
		return Stop( cancellation );
	}
	internal static void Command( IModuleRegistration registration, string name, CommandHandler handler = null )
		=> registration.RegisterCommand( new CommandDescriptor( name, "Test command", "test.use", registration.ModuleId ),
			handler ?? (( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Executed." ) )) );
}
