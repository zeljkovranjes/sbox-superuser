using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Commands;

internal static class DogCommands
{
	/// <summary>Demonstrates a game-specific capability without assuming the game has dogs.</summary>
	[AdminCommand( Name = "dog.spawn", Description = "Spawn a dog for a player", Permission = "dog.spawn", ModuleId = "sample.dogs", Aliases = new[] { "dog" } )]
	[AdminArgument( "target", Description = "Online recipient", TargetPolicy = CommandTargetPolicy.OnlinePlayer )]
	[AdminArgument( "count", Description = "Number of dogs", Minimum = 1, Maximum = 5 )]
	public static CommandResult Spawn( CommandContext context, PlayerReference target, int count = 1 )
	{
		var spawner = context.Services.GetService<IDogSpawner>();
		return spawner is null ? new CommandResult( CommandStatus.Unsupported, "This game has no dog spawner." ) : spawner.Spawn( target, count );
	}

	/// <summary>Returns converted arguments for the development verification script.</summary>
	[AdminCommand( Name = "dog.types", Description = "Verify supported typed parameters", Permission = "dog.spawn", ModuleId = "sample.dogs", ExecutionPolicy = CommandExecutionPolicy.ReadOnly )]
	public static System.Threading.Tasks.Task<CommandResult> Types( CommandContext context, string text, int count, long large,
		float small, double number, bool enabled, DogMood mood, CommandDuration duration )
	{
		return System.Threading.Tasks.Task.FromResult( new CommandResult( CommandStatus.Success,
			$"{text}|{count}|{large}|{small}|{number}|{enabled}|{mood}|{duration.IsPermanent}" ) );
	}
}
