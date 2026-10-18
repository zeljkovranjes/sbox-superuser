using SboxSuperuser.Engine.Audit;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class HostCommandDispatcher
{
	private static AuditAction CreateAuditAction( CommandContext context, CommandDescriptor descriptor, CommandArguments arguments )
	{
		return new AuditAction( context.Actor, context.Origin, context.RequestId, descriptor.Name, DateTimeOffset.UtcNow,
			AuditRedaction.Arguments( descriptor, arguments ), AuditRedaction.Targets( descriptor, arguments ) );
	}

	private async Task<CommandResult> ExecuteAuditedAsync( CommandContext context, CommandExecutionServices services,
		string entryName, RegisteredCommand command, CommandArguments arguments, AuditSubmission audit )
	{
		var rejected = ValidateExecution( context, services, entryName, command, arguments );
		if ( rejected is not null || _audit is null || !command.RecordsAudit )
		{
			return rejected ?? await ExecuteAsync( context, services, entryName, command, arguments );
		}

		audit.Handled = true;
		if ( !await _audit.BeginAsync( audit.Action, context.Cancellation ) )
		{
			return Result( CommandStatus.StorageUnavailable, "A durable audit intent is required before execution." );
		}

		// ExecuteAsync rechecks authority and targets after the awaited intent write.
		var result = await ExecuteAsync( context, services, entryName, command, arguments );
		return await _audit.CompleteAsync( audit.Action, result.Status, CancellationToken.None ) ? result
			: Result( CommandStatus.Unknown, "The action has no confirmed durable outcome; inspect audit health before retrying." );
	}
}
