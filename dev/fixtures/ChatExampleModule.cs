using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Chat;

/// <summary>Development command proving both chat transports share one dispatcher and authenticated actor.</summary>
public sealed class ChatExampleModule : IAdminModule
{
	private int _executions;
	/// <summary>Gets the example module contract.</summary>
	public ModuleDescriptor Descriptor { get; } = new( "example.chat", "Custom chat integration fixture", 1 );

	/// <summary>Registers a counted command using the existing default player help permission.</summary>
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		registration.RegisterCommand( new CommandDescriptor( "example.increment", "Count one authenticated example command", "admin.help", Descriptor.Id,
			new[] { new CommandArgumentDescriptor( "tag", "Private test marker", CommandArgumentKind.String ) } ),
			( context, arguments ) =>
			{
				_executions++;
				return Task.FromResult( new CommandResult( CommandStatus.Success,
					$"execution={_executions}; actor={context.Actor.SubjectId}; tag={arguments.Get<string>( "tag" )}" ) );
			} );
		return Task.CompletedTask;
	}

	/// <summary>Leaves the sample count in its game-owned instance.</summary>
	public Task StopAsync( CancellationToken cancellation ) => Task.CompletedTask;
}
