using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Modules;

/// <summary>A removable sample; its game-owned pulse source survives module disable.</summary>
public sealed class SampleCounterModule : IAdminModule
{
	private readonly SamplePulseSource _source;
	/// <summary>Gets the module contract and permission ownership.</summary>
	public ModuleDescriptor Descriptor { get; } = new( "sample.counter", "Counts game pulses and command executions", 1 );
	/// <summary>Gets pulses observed through the currently tracked listener.</summary>
	public int Pulses { get; private set; }
	/// <summary>Gets command executions across enable/disable cycles.</summary>
	public int Executions { get; private set; }
	/// <summary>Gets completed cleanup calls.</summary>
	public int Stops { get; private set; }

	/// <summary>Uses an existing game-owned event source.</summary>
	public SampleCounterModule( SamplePulseSource source ) => _source = source;

	/// <summary>Stages one permission, command and disposable subscription.</summary>
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		cancellation.ThrowIfCancellationRequested();
		registration.RegisterPermission( new PermissionDescriptor( "sample.counter.use", "Read the sample counter" ) );
		registration.RegisterCommand( new CommandDescriptor( "sample.count", "Count an execution and report game pulses", "sample.counter.use", Descriptor.Id ),
			( context, _ ) =>
			{
				context.Cancellation.ThrowIfCancellationRequested();
				Executions++;
				return Task.FromResult( new CommandResult( CommandStatus.Success, $"executions={Executions}; pulses={Pulses}" ) );
			} );
		registration.Track( _source.Subscribe( () => Pulses++ ) );
		return Task.CompletedTask;
	}

	/// <summary>Completes after the manager removes listeners and drains handlers.</summary>
	public Task StopAsync( CancellationToken cancellation )
	{
		Stops++;
		return Task.CompletedTask;
	}
}
