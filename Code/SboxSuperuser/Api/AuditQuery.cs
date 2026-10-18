using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser;

/// <summary>Immutable bounded audit filters; the host applies authorization independently.</summary>
public sealed class AuditQuery
{
	/// <summary>Gets the maximum returned rows, from 1 through 100.</summary>
	public int PageSize { get; }
	/// <summary>Gets the prior response cursor, or null for the newest page.</summary>
	public AuditCursor Cursor { get; }
	/// <summary>Gets an optional exact request ID filter.</summary>
	public Guid? RequestId { get; }
	/// <summary>Gets an optional exact command key filter.</summary>
	public string Command { get; }
	/// <summary>Gets an optional exact actor subject filter.</summary>
	public string ActorSubject { get; }
	/// <summary>Gets an optional outcome filter.</summary>
	public CommandStatus? Outcome { get; }
	/// <summary>Gets an optional authenticated source filter.</summary>
	public CommandOrigin? Origin { get; }
	/// <summary>Gets an inclusive UTC creation lower bound.</summary>
	public DateTimeOffset? FromUtc { get; }
	/// <summary>Gets an inclusive UTC creation upper bound.</summary>
	public DateTimeOffset? ToUtc { get; }
	/// <summary>Gets an optional exact affected player ID filter.</summary>
	public string TargetSteamId { get; }

	/// <summary>Creates validated filters without reading or authorizing access to the audit store.</summary>
	public AuditQuery( int pageSize = 25, AuditCursor cursor = null, Guid? requestId = null, string command = null,
		string actorSubject = null, CommandStatus? outcome = null, CommandOrigin? origin = null,
		DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, string targetSteamId = null )
	{
		if ( pageSize is < 1 or > 100 || requestId == Guid.Empty || fromUtc > toUtc
			|| fromUtc.HasValue && fromUtc.Value.Offset != TimeSpan.Zero || toUtc.HasValue && toUtc.Value.Offset != TimeSpan.Zero )
		{
			throw new ArgumentException( "Audit pagination, request ID or UTC range is invalid." );
		}

		if ( command is not null )
		{
			ContractGuard.Key( command, nameof( command ) );
		}

		if ( command?.Length > 96 || actorSubject is not null && (string.IsNullOrWhiteSpace( actorSubject ) || actorSubject.Length > 96) )
		{
			throw new ArgumentException( "Audit filters exceed their limits." );
		}

		if ( targetSteamId is not null )
		{
			SteamIdentity.Validate( targetSteamId, nameof( targetSteamId ) );
		}

		if ( outcome.HasValue )
		{
			ContractGuard.EnumValue( outcome.Value, nameof( outcome ) );
		}
		if ( origin.HasValue )
		{
			ContractGuard.EnumValue( origin.Value, nameof( origin ) );
		}
		PageSize = pageSize;
		Cursor = cursor;
		RequestId = requestId;
		Command = command;
		ActorSubject = actorSubject;
		Outcome = outcome;
		Origin = origin;
		FromUtc = fromUtc;
		ToUtc = toUtc;
		TargetSteamId = targetSteamId;
	}
}
