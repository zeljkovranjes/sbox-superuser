using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Audit;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Engine.ChatIntegration;

internal sealed class ChatCommandIngress : IChatCommandIngress
{
	private readonly ChatCommandPrefix _prefix;
	private readonly HostCommandDispatcher _dispatcher;
	private readonly HostAuditService _audit;
	private readonly Func<bool> _available;

	internal ChatCommandIngress( string root, HostCommandDispatcher dispatcher, HostAuditService audit, Func<bool> available )
	{
		_prefix = new ChatCommandPrefix( root );
		_dispatcher = dispatcher;
		_audit = audit;
		_available = available;
	}

	public bool Owns( string message ) => _prefix.Owns( message );

	public async Task<CommandResult> ExecuteAsync( Func<ActorIdentity> authenticate, string message, Guid requestId, CancellationToken cancellation )
	{
		if ( !_available() || !Owns( message ) || requestId == Guid.Empty )
		{
			return new CommandResult( CommandStatus.Unsupported, "Chat command ingress is unavailable." );
		}
		var actor = authenticate();
		if ( actor is null || actor.Kind != ActorKind.Player )
		{
			return new CommandResult( CommandStatus.Denied, "An authenticated player connection is required." );
		}

		var parsed = _prefix.Parse( message );
		CommandRoute route = null;
		if ( parsed.Error is null )
		{
			try
			{
				route = CommandRoute.Resolve( parsed.Tokens );
			}
			catch ( ArgumentException )
			{
				// Invalid text is never copied to diagnostics or audit arguments.
			}
		}
		if ( route is null )
		{
			await _audit.RecordAsync( new AuditAction( actor, CommandOrigin.Chat, requestId, "command.invalid", DateTimeOffset.UtcNow ),
				CommandStatus.InvalidArguments, cancellation );
			return new CommandResult( CommandStatus.InvalidArguments, $"Malformed or oversized command. Use {_prefix.Value} help for syntax." );
		}

		return await _dispatcher.SubmitAsync( authenticate, CommandOrigin.Chat,
			new CommandRequest( route.Command, route.Arguments, requestId ), cancellation );
	}
}
