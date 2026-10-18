using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Host-issued execution context; transports must not deserialize this from client input.</summary>
public sealed class CommandContext
{
	/// <summary>Gets the authenticated principal; roles must still be checked when executing.</summary>
	public ActorIdentity Actor { get; }

	/// <summary>Gets the authenticated ingress transport.</summary>
	public CommandOrigin Origin { get; }

	/// <summary>Gets the request identifier, scoped by actor and origin for future deduplication.</summary>
	public Guid RequestId { get; }

	/// <summary>Gets the cancellation signal; cancellation never promises rollback.</summary>
	public CancellationToken Cancellation { get; }

	/// <summary>Gets approved optional capabilities, independent of any game's player system.</summary>
	public ICommandServices Services { get; }

	internal CommandContext( ActorIdentity actor, CommandOrigin origin, Guid requestId,
		CancellationToken cancellation, ICommandServices services )
	{
		Actor = actor ?? throw new ArgumentNullException( nameof( actor ) );
		Origin = ContractGuard.EnumValue( origin, nameof( origin ) );
		if ( origin == CommandOrigin.Unknown || requestId == Guid.Empty )
		{
			throw new ArgumentException( "An explicit origin and request ID are required." );
		}

		RequestId = requestId;
		Cancellation = cancellation;
		Services = services ?? throw new ArgumentNullException( nameof( services ) );
	}
}
