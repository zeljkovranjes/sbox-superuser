using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Modules;

/// <summary>Development-only failure injection after registering a listener and command.</summary>
public sealed class FailingStartupModule : IAdminModule
{
	private readonly SamplePulseSource _source;
	/// <summary>Gets the test module metadata.</summary>
	public ModuleDescriptor Descriptor { get; } = new( "sample.failure", "Injected startup failure", 1 );
	/// <summary>Uses the same signal source as the removable sample.</summary>
	public FailingStartupModule( SamplePulseSource source ) => _source = source;
	/// <summary>Stages contributions, then fails before publication.</summary>
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		registration.RegisterPermission( new PermissionDescriptor( "sample.failure.use", "Failure test" ) );
		registration.RegisterCommand( new CommandDescriptor( "sample.failure", "Must never execute", "sample.failure.use", Descriptor.Id ),
			( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Unexpected execution" ) ) );
		registration.Track( _source.Subscribe( () => { } ) );
		throw new InvalidOperationException( "Intentional module startup failure." );
	}
	/// <summary>Allows the manager to finish rollback after the injected failure.</summary>
	public Task StopAsync( CancellationToken cancellation ) => Task.CompletedTask;
}
