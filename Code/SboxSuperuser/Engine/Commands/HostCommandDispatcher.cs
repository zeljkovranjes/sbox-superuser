using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Authority;
using SboxSuperuser.Engine.Audit;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class HostCommandDispatcher : ICommandDispatcher, IDisposable
{
	private readonly HostPolicyService _policy;
	private readonly CommandRegistry _registry;
	private readonly Func<bool> _available;
	private readonly Func<IReadOnlyList<CommandPlayer>> _players;
	private readonly Action<Exception> _report;
	private readonly CancellationTokenSource _lifetime = new();
	private readonly RequestQueue<CommandResult> _queue;
	private bool _disposed;
	private readonly IAuditWriter _audit;

	internal HostCommandDispatcher( HostPolicyService policy, CommandRegistry registry, Func<bool> available,
		Func<IReadOnlyList<CommandPlayer>> players, Action<Exception> report, Func<DateTimeOffset> clock = null, IAuditWriter audit = null )
	{
		_policy = policy;
		_registry = registry;
		_available = available;
		_players = players;
		_report = report;
		_audit = audit;
		_queue = new RequestQueue<CommandResult>( clock ?? (() => DateTimeOffset.UtcNow), Result( CommandStatus.Cancelled, "Cancelled before execution." ) );
	}

	internal async Task<CommandResult> SubmitAsync( Func<ActorIdentity> authenticate, CommandOrigin origin, CommandRequest request,
		CancellationToken cancellation, CommandService service = null )
	{
		if ( !_available() || _disposed )
		{
			return Result( CommandStatus.Unsupported, "An eligible authoritative runtime on the main thread is required." );
		}

		if ( request is null )
		{
			return Result( CommandStatus.InvalidArguments, "A command request is required." );
		}

		using var linked = CancellationTokenSource.CreateLinkedTokenSource( cancellation, _lifetime.Token );
		var actor = authenticate();
		if ( actor is null )
		{
			return Result( CommandStatus.Denied, "The caller could not be authenticated." );
		}

		var context = new CommandContext( actor, origin, request.RequestId, linked.Token, new CommandExecutionServices( this, authenticate, service ) );
		return await DispatchAsync( context, request );
	}

	public async Task<CommandResult> DispatchAsync( CommandContext context, CommandRequest request )
	{
		var audit = new AuditSubmission();
		var result = await DispatchCoreAsync( context, request, audit );
		if ( _audit is not null && audit.Action is not null && !audit.Handled )
		{
			await _audit.RecordAsync( audit.Action, result.Status, CancellationToken.None );
		}

		return result;
	}

	private async Task<CommandResult> DispatchCoreAsync( CommandContext context, CommandRequest request, AuditSubmission audit )
	{
		try
		{
			if ( context is null || request is null || context.RequestId != request.RequestId
				|| context.Services is not CommandExecutionServices services || !ReferenceEquals( services.Issuer, this ) )
			{
				return Result( CommandStatus.Denied, "Host-issued matching request context is required." );
			}

			var authorization = ValidateCaller( context, services );
			// Only a still-authenticated transport identity may be attributed in the journal.
			if ( authorization is null )
			{
				audit.Action = new AuditAction( context.Actor, context.Origin, request.RequestId,
					"command.unresolved", DateTimeOffset.UtcNow );
			}
			if ( authorization is not null )
			{
				return authorization;
			}

			if ( request.Command.Length > 96 || CommandTokenizer.ValidateTokens( request.Arguments ) is not null )
			{
				return Result( CommandStatus.InvalidArguments, "Command input exceeds its limits or contains unsupported characters." );
			}

			var key = new RequestKey( (int)context.Actor.Kind, context.Actor.SubjectId, (int)context.Origin, context.RequestId );
			var fingerprint = RequestFingerprint.Compute( request.Command, request.Arguments );
			var retry = _queue.FindRetry( key, fingerprint );
			if ( retry is not null )
			{
				audit.Handled = retry.Admission != QueueAdmission.Conflict;
				return retry.Admission == QueueAdmission.Conflict
					? Result( CommandStatus.Conflict, "This request ID was already used with a different payload." ) : await retry.Result;
			}

			var command = _registry.Find( request.Command );
			if ( command is null )
			{
				return Result( CommandStatus.NotFound, "Command is not registered." );
			}

			audit.Action = CreateAuditAction( context, command.Descriptor, null );
			if ( context.Origin == CommandOrigin.Chat && command.Descriptor.Arguments.Any( argument =>
				argument.Audit == CommandArgumentAudit.Sensitive || AuditRedaction.SensitiveName( argument.Name ) ) )
			{
				return Result( CommandStatus.Denied, "Sensitive commands require a private non-chat transport." );
			}

			if ( !HasPermission( context, services, command.Descriptor.Permission ) )
			{
				return Result( CommandStatus.Denied, "Permission denied." );
			}

			var parsed = CommandArgumentParser.Parse( command.Descriptor, request.Arguments, _players() );
			if ( !parsed.IsSuccess )
			{
				return new CommandResult( CommandStatus.InvalidArguments, parsed.Error, parsed.PlayerChoices.Count == 0 ? null
					: new Dictionary<string, string> { [ "choices" ] = string.Join( ",", parsed.PlayerChoices.Select( player => player.SteamId ) ) } );
			}

			audit.Action = CreateAuditAction( context, command.Descriptor, parsed.Arguments );
			var admission = _queue.Submit( key, fingerprint,
				() => ExecuteAuditedAsync( context, services, request.Command, command, parsed.Arguments, audit ), context.Cancellation );
			return admission.Admission switch
			{
				QueueAdmission.Conflict => Result( CommandStatus.Conflict, "This request ID was already used with a different payload." ),
				QueueAdmission.Busy => Result( CommandStatus.Conflict, "Command capacity is full; retry later." ),
				_ => await admission.Result
			};
		}
		catch ( Exception error )
		{
			_report( error );
			return Result( CommandStatus.Unknown, "The command outcome could not be determined. See host diagnostics." );
		}
	}

	public void Dispose()
	{
		if ( _disposed )
		{
			return;
		}

		_disposed = true;
		_lifetime.Cancel();
		_lifetime.Dispose();
	}

	private static CommandResult Result( CommandStatus status, string message ) => new( status, message );
}
