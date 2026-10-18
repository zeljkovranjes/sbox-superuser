namespace SboxSuperuser.Engine.Commands;

// Host-issued contexts only. The implementation reauthenticates before admission and execution;
// public transports enter through the facade, never by deserializing a context.
internal interface ICommandDispatcher
{
	Task<CommandResult> DispatchAsync( CommandContext context, CommandRequest request );
}
