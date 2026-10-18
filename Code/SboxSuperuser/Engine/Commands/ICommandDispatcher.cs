namespace SboxSuperuser.Engine.Commands;

// T09 boundary: implementations authenticate context, match request IDs, authorize at execution,
// parse, serialize mutations and audit before returning. No implementation exists in T02.
internal interface ICommandDispatcher
{
	Task<CommandResult> DispatchAsync( CommandContext context, CommandRequest request );
}
