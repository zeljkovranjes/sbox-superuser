using SboxSuperuser;
using System.Threading;
using System.Threading.Tasks;

namespace SuperuserExamples.Engine;

/// <summary>A minimal command module for the getting-started guide.</summary>
public sealed class PingModule : IAdminModule
{
	/// <summary>Declares the module's name and supported API version.</summary>
	public ModuleDescriptor Descriptor { get; } = new( "example.ping", "A ping command", 1 );

	/// <summary>Registers the permission and attributed command.</summary>
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		cancellation.ThrowIfCancellationRequested();
		registration.RegisterPermission( new PermissionDescriptor( "example.ping", "Use ping" ) );
		registration.DiscoverCommands();
		return Task.CompletedTask;
	}

	/// <summary>No additional resources need cleanup.</summary>
	public Task StopAsync( CancellationToken cancellation ) => Task.CompletedTask;

	/// <summary>Returns a private reply to the caller.</summary>
	[AdminCommand( Name = "ping", Description = "Reply with Pong!", Permission = "example.ping",
		ModuleId = "example.ping", ExecutionPolicy = CommandExecutionPolicy.ReadOnly )]
	public static CommandResult Ping( CommandContext context )
	{
		return new CommandResult( CommandStatus.Success, "Pong!" );
	}
}
