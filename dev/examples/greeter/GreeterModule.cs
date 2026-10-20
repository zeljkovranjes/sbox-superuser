using SboxSuperuser;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperuserExamples.Engine;

/// <summary>
/// A complete third-party module in one file: it loads itself, registers a permission and two commands, and
/// documents itself. Drop its folder into your game's Code/Superuser/Modules folder and it works.
/// </summary>
[AdminModule]
public sealed class GreeterModule : IAdminModule
{
	private const string Permission = "example.greeter.use";

	/// <summary>Who the module is and who made it; shown on its Overview page.</summary>
	public ModuleDescriptor Descriptor { get; } = new( "example.greeter", "Says hello and rolls dice for players", 1 )
	{
		Version = "1.0.0",
		Author = "Superuser examples",
		Website = "https://github.com/zeljkovranjes/sbox-superuser"
	};

	/// <summary>Registers everything the module offers; Superuser publishes it all at once when this returns.</summary>
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		registration.RegisterPermission( new PermissionDescriptor( Permission, "Use the greeter's hello and roll commands" ) );

		registration.RegisterCommand(
			new CommandDescriptor( "hello", "Say hello back", Permission, Descriptor.Id,
				executionPolicy: CommandExecutionPolicy.ReadOnly ),
			( context, arguments ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Hello! The greeter module is running." ) ) );

		registration.RegisterCommand(
			new CommandDescriptor( "roll", "Roll a die", Permission, Descriptor.Id,
				new[] { new CommandArgumentDescriptor( "sides", "How many sides the die has", CommandArgumentKind.Integer,
					isOptional: true, minimum: 2, maximum: 1000, audit: CommandArgumentAudit.SafeValue ) },
				executionPolicy: CommandExecutionPolicy.ReadOnly ),
			( context, arguments ) => Task.FromResult( Roll( arguments ) ) );

		registration.AddDocumentation( "Using the greeter", """
			# Using the greeter

			The greeter is the smallest useful module. Try it from the console:

			```text
			admin hello
			admin roll 20
			```

			Both commands need the `example.greeter.use` permission. Give it to everyone with
			`admin group player permission allow example.greeter.use`.
			""" );

		registration.AddMenuWindow( "Greeter", window => window
			.Text( "The smallest useful module, with its own window in the admin menu." )
			.Button( "Say hello", "hello" )
			.CommandForm( "roll" ) );
		return Task.CompletedTask;
	}

	/// <summary>Nothing to clean up: Superuser removes the commands and permission itself.</summary>
	public Task StopAsync( CancellationToken cancellation ) => Task.CompletedTask;

	private static CommandResult Roll( CommandArguments arguments )
	{
		var sides = arguments.Contains( "sides" ) ? (int)arguments.Get<long>( "sides" ) : 6;
		var rolled = Random.Shared.Next( 1, sides + 1 );
		return new CommandResult( CommandStatus.Success, $"You rolled {rolled} (1-{sides})." );
	}
}
