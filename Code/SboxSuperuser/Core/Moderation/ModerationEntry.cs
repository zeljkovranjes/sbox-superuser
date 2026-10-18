using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Moderation;

internal sealed class ModerationEntry
{
	internal Guid Id { get; }
	internal Guid RequestId { get; }
	internal string Target { get; }
	internal ModerationKind Kind { get; }
	internal string ActorKind { get; }
	internal string ActorSubject { get; }
	internal string Origin { get; }
	internal string Reason { get; }
	internal DateTimeOffset CreatedUtc { get; }
	internal DateTimeOffset? ExpiresUtc { get; }
	internal Guid? Revokes { get; }

	internal ModerationEntry( Guid id, Guid requestId, string target, ModerationKind kind,
		string actorKind, string actorSubject, string origin, string reason, DateTimeOffset createdUtc,
		DateTimeOffset? expiresUtc = null, Guid? revokes = null )
	{
		if ( id == Guid.Empty || requestId == Guid.Empty || revokes == Guid.Empty )
			throw new ArgumentException( "Moderation requires explicit action and request IDs." );
		Target = SteamIdentity.Validate( target, nameof( target ) );
		Kind = ContractGuard.EnumValue( kind, nameof( kind ) );
		if ( actorKind == "Player" ) SteamIdentity.Validate( actorSubject, nameof( actorSubject ) );
		else if ( actorKind == "InternalService" ) ContractGuard.Key( actorSubject, nameof( actorSubject ) );
		else if ( actorKind != "DedicatedConsole" || actorSubject != "dedicated-console" )
			throw new ArgumentException( "Invalid moderation actor." );
		if ( origin is not ("Console" or "Chat" or "InternalService")
			|| actorSubject.Length > 96 || string.IsNullOrWhiteSpace( reason ) || reason.Length > 256 || reason.Any( char.IsControl ) )
			throw new ArgumentException( "Moderation requires a valid origin and a reason of 1–256 printable characters." );
		if ( createdUtc.Offset != TimeSpan.Zero || expiresUtc.HasValue && (expiresUtc.Value.Offset != TimeSpan.Zero || expiresUtc <= createdUtc)
			|| expiresUtc.HasValue && kind is not (ModerationKind.Ban or ModerationKind.TextMute)
			|| revokes.HasValue != (kind is ModerationKind.Unban or ModerationKind.Unmute) )
			throw new ArgumentException( "Invalid UTC expiry or revocation contract." );
		Id = id;
		RequestId = requestId;
		ActorKind = actorKind;
		ActorSubject = actorSubject;
		Origin = origin;
		Reason = reason;
		CreatedUtc = createdUtc;
		ExpiresUtc = expiresUtc;
		Revokes = revokes;
	}
}
