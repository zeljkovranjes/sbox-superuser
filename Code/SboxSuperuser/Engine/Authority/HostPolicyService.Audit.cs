using SboxSuperuser.Engine.Audit;

namespace SboxSuperuser.Engine.Authority;

internal sealed partial class HostPolicyService
{
	private async Task<CommandResult> AuditedMutationAsync( Func<ActorIdentity> authenticate, CommandOrigin origin,
		Guid requestId, string command, IEnumerable<string> targets, Func<Task<CommandResult>> execute )
	{
		var actor = authenticate();
		if ( _audit is null || actor is null )
		{
			return await execute();
		}

		var action = new AuditAction( actor, origin, requestId, command, DateTimeOffset.UtcNow, targets: targets );
		if ( !await _audit.BeginAsync( action, CancellationToken.None ) )
		{
			return new CommandResult( CommandStatus.StorageUnavailable, "A durable audit intent is required before changing authority." );
		}

		var current = authenticate();
		var result = current is null || actor.Kind != current.Kind || actor.SubjectId != current.SubjectId
			? new CommandResult( CommandStatus.Denied, "Caller identity changed while persisting the audit intent." )
			: await execute();
		return await _audit.CompleteAsync( action, result.Status, CancellationToken.None ) ? result
			: new CommandResult( CommandStatus.Unknown, "Authority operation has no confirmed durable audit outcome." );
	}
}
