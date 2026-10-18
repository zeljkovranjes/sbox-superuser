using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Modules;

/// <summary>Development-only handler that waits for explicit release to test bounded shutdown.</summary>
public sealed class HeldCommandModule : IAdminModule
{
	private TaskCompletionSource<bool> _release;
	private CancellationToken _activeCancellation;
	/// <summary>Gets test metadata.</summary>
	public ModuleDescriptor Descriptor { get; } = new( "sample.held", "Bounded drain fixture", 1 );
	/// <summary>Gets whether the command has reached its hold point.</summary>
	public bool Entered { get; private set; }
	/// <summary>Gets whether shutdown signaled the running command.</summary>
	public bool CancellationSeen => _activeCancellation.IsCancellationRequested;
	/// <summary>Gets cleanup completions.</summary>
	public int Stops { get; private set; }
	/// <summary>Stages a handler that intentionally ignores cancellation until released.</summary>
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		Entered = false;
		_release = new TaskCompletionSource<bool>();
		registration.RegisterPermission( new PermissionDescriptor( "sample.held.use", "Run drain fixture" ) );
		registration.RegisterCommand( new CommandDescriptor( "sample.hold", "Hold until the test releases it", "sample.held.use", Descriptor.Id ),
			async ( context, _ ) =>
			{
				_activeCancellation = context.Cancellation;
				Entered = true;
				await _release.Task;
				return new CommandResult( CommandStatus.Success, "Released; dispatcher must report interrupted mutation as Unknown." );
			} );
		return Task.CompletedTask;
	}
	/// <summary>Releases the development hold point without simulating a forced cancellation.</summary>
	public void Release() => _release.TrySetResult( true );
	/// <summary>Records cleanup after active execution has ended.</summary>
	public Task StopAsync( CancellationToken cancellation )
	{
		Stops++;
		return Task.CompletedTask;
	}
}
