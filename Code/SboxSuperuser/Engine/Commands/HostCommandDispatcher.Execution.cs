namespace SboxSuperuser.Engine.Commands;

internal sealed partial class HostCommandDispatcher
{
	private async Task<CommandResult> ExecuteAsync( CommandContext original, CommandExecutionServices services,
		string entryName, RegisteredCommand command, CommandArguments arguments )
	{
		var rejected = ValidateCaller( original, services );
		if ( rejected is not null )
		{
			return rejected;
		}

		if ( !ReferenceEquals( _registry.Find( entryName ), command ) )
		{
			return Result( CommandStatus.Conflict, "Command registration changed before execution; submit a new request." );
		}

		if ( !HasPermission( original, services, command.Descriptor.Permission ) )
		{
			return Result( CommandStatus.Denied, "Permission was revoked before execution." );
		}

		var targetError = ValidateTargets( original.Actor, command.Descriptor, arguments );
		if ( targetError is not null )
		{
			return targetError;
		}

		var context = new CommandContext( original.Actor, original.Origin, original.RequestId, original.Cancellation,
			new CommandExecutionServices( this, services.Authenticate, services.Service, command.Capabilities ) );
		try
		{
			var result = await command.Handler( context, arguments );
			if ( context.Cancellation.IsCancellationRequested || !_available() )
			{
				return Result( command.Descriptor.ExecutionPolicy == CommandExecutionPolicy.Mutation ? CommandStatus.Unknown : CommandStatus.Cancelled,
					"Execution was interrupted; no rollback is implied." );
			}

			return IsBounded( result ) ? result : Result( CommandStatus.Unknown, "The handler returned an invalid or oversized result." );
		}
		catch ( OperationCanceledException )
		{
			return Result( command.Descriptor.ExecutionPolicy == CommandExecutionPolicy.Mutation ? CommandStatus.Unknown : CommandStatus.Cancelled,
				"Execution was interrupted; no rollback is implied." );
		}
		catch ( Exception error )
		{
			_report( error );
			return Result( command.Descriptor.ExecutionPolicy == CommandExecutionPolicy.Mutation ? CommandStatus.Unknown : CommandStatus.Failed,
				"The handler failed. See host diagnostics; no rollback is implied." );
		}
	}

	private CommandResult ValidateCaller( CommandContext context, CommandExecutionServices services )
	{
		if ( !_available() || _lifetime.IsCancellationRequested )
		{
			return Result( CommandStatus.Unsupported, "The authoritative runtime is no longer available." );
		}

		if ( context.Cancellation.IsCancellationRequested )
		{
			return Result( CommandStatus.Cancelled, "Cancelled before execution." );
		}

		var current = services.Authenticate();
		if ( current is null || current.Kind != context.Actor.Kind || current.SubjectId != context.Actor.SubjectId
			|| current.Kind == ActorKind.Player && context.Origin is not (CommandOrigin.Console or CommandOrigin.Chat)
			|| current.Kind == ActorKind.DedicatedConsole && context.Origin != CommandOrigin.Console
			|| current.Kind == ActorKind.InternalService && (context.Origin != CommandOrigin.InternalService || services.Service is null
				|| !services.Service.IsActive || services.Service.Id != current.SubjectId) )
		{
			return Result( CommandStatus.Denied, "Caller authority is no longer valid for this transport." );
		}

		return !_policy.IsUsable ? Result( CommandStatus.StorageUnavailable, "Authority state is unavailable; access denied." ) : null;
	}

	private bool HasPermission( CommandContext context, CommandExecutionServices services, string permission )
	{
		var explanation = _policy.Explain( context.Actor, permission );
		return context.Actor.Kind == ActorKind.InternalService
			? _policy.IsUsable && explanation.IsRegistered && services.Service?.Allows( permission ) == true
			: explanation.IsAllowed;
	}

	private CommandResult ValidateTargets( ActorIdentity actor, CommandDescriptor descriptor, CommandArguments arguments )
	{
		var players = _players();
		if ( players is null || players.Count > 1024 )
		{
			return Result( CommandStatus.Unsupported, "The online player directory is unavailable." );
		}

		foreach ( var argument in descriptor.Arguments.Where( value => value.Kind == CommandArgumentKind.Player ) )
		{
			if ( !arguments.Contains( argument.Name ) )
			{
				continue;
			}

			var target = arguments.Get<PlayerReference>( argument.Name ).SteamId;
			if ( argument.TargetPolicy is CommandTargetPolicy.OnlinePlayer or CommandTargetPolicy.PunitiveOnlinePlayer
				&& !players.Any( player => player.SteamId == target ) )
			{
				return Result( CommandStatus.NotFound, "A required target is no longer online." );
			}

			if ( argument.TargetPolicy is CommandTargetPolicy.PunitiveAnyPlayer or CommandTargetPolicy.PunitiveOnlinePlayer
				&& !_policy.CheckTarget( actor, target ).IsAllowed )
			{
				return Result( CommandStatus.Denied, "Target protection denied this action." );
			}
		}

		return null;
	}

	private static bool IsBounded( CommandResult result ) => result is not null && result.Message.Length <= 1024
		&& result.Data.Count <= 16 && result.Data.All( pair => pair.Key.Length <= 64 && pair.Value.Length <= 1024 )
		&& result.Data.Sum( pair => pair.Key.Length + pair.Value.Length ) <= 8192;
}
