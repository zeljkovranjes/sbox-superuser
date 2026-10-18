using SboxSuperuser;
using System;
using System.Threading.Tasks;

namespace SuperuserCompatibilityLab.Engine.Commands;

// Development-only counter commands use the default help permission to avoid modifying persisted lab policy.
internal static class DispatchCommands
{
	/// <summary>Runs a compiled game capability through authenticated dispatch.</summary>
	[AdminCommand( Name = "probe.increment", Description = "Increment test counter", Permission = "admin.help", ModuleId = "test.dispatch" )]
	public static CommandResult Increment( CommandContext context ) => context.Services.GetService<IDispatchProbe>()?.Increment()
		?? new CommandResult( CommandStatus.Unsupported, "No counter capability" );

	/// <summary>Holds the serial dispatcher for queued-request tests.</summary>
	[AdminCommand( Name = "probe.hold", Description = "Hold test queue", Permission = "admin.help", ModuleId = "test.dispatch" )]
	public static Task<CommandResult> Hold( CommandContext context ) => context.Services.GetService<IDispatchProbe>()?.WaitAsync()
		?? Task.FromResult( new CommandResult( CommandStatus.Unsupported, "No barrier capability" ) );

	/// <summary>Injects a handler failure with text that must not reach the caller.</summary>
	[AdminCommand( Name = "probe.fail", Description = "Inject test failure", Permission = "admin.help", ModuleId = "test.dispatch" )]
	public static CommandResult Fail( CommandContext context ) => throw new InvalidOperationException( "test-secret-must-not-escape" );

	/// <summary>Exercises punitive protection without performing moderation.</summary>
	[AdminCommand( Name = "probe.punish", Description = "Check test target protection", Permission = "admin.help", ModuleId = "test.dispatch" )]
	[AdminArgument( "target", TargetPolicy = CommandTargetPolicy.PunitiveAnyPlayer )]
	public static CommandResult Punish( CommandContext context, PlayerReference target ) => context.Services.GetService<IDispatchProbe>()?.Increment()
		?? new CommandResult( CommandStatus.Unsupported, "No counter capability" );
}
