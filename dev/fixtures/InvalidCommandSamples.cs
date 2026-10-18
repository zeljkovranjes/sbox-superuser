using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Commands;

internal sealed class InvalidCommandSamples
{
	/// <summary>Invalid instance handler used only to verify registration failure.</summary>
	[AdminCommand( Name = "bad.instance", Description = "Invalid", Permission = "dog.spawn", ModuleId = "invalid.instance" )]
	public CommandResult Instance( CommandContext context ) => new( CommandStatus.Success, "Must not execute" );

	/// <summary>Invalid return type used only to verify registration failure.</summary>
	[AdminCommand( Name = "bad.void", Description = "Invalid", Permission = "dog.spawn", ModuleId = "invalid.void" )]
	public static void Void( CommandContext context ) { }

	/// <summary>Unsupported mutable argument used only to verify registration failure.</summary>
	[AdminCommand( Name = "bad.type", Description = "Invalid", Permission = "dog.spawn", ModuleId = "invalid.type" )]
	public static CommandResult Type( CommandContext context, object arbitrary ) => new( CommandStatus.Success, "Must not execute" );

	/// <summary>Open generic method used only to verify registration failure.</summary>
	[AdminCommand( Name = "bad.generic", Description = "Invalid", Permission = "dog.spawn", ModuleId = "invalid.generic" )]
	public static CommandResult Generic<T>( CommandContext context ) => new( CommandStatus.Success, "Must not execute" );

	/// <summary>Reference argument used only to verify registration failure.</summary>
	[AdminCommand( Name = "bad.ref", Description = "Invalid", Permission = "dog.spawn", ModuleId = "invalid.ref" )]
	public static CommandResult Ref( CommandContext context, ref int value ) => new( CommandStatus.Success, "Must not execute" );
}
