using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser;

/// <summary>Exact-target moderation input; contains no actor, permission or console text.</summary>
public sealed class AdminActionRequest
{
	/// <summary>Gets the moderation operation.</summary>
	public AdminAction Action { get; }

	/// <summary>Gets the canonical target Steam ID; names are never used for mutations.</summary>
	public string SteamId { get; }

	/// <summary>Gets the operator's printable reason.</summary>
	public string Reason { get; }

	/// <summary>Gets an explicit duration such as 30m, 7d or permanent for ban/mute.</summary>
	public string Duration { get; }

	/// <summary>Creates bounded input; permission, duration parsing and target protection remain host-side.</summary>
	public AdminActionRequest( AdminAction action, string steamId, string reason, string duration = "" )
	{
		ContractGuard.EnumValue( action, nameof( action ) );
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		if ( string.IsNullOrWhiteSpace( reason ) || reason.Length > 256 || reason.Any( char.IsControl )
			|| duration is null || duration.Length > 32 || duration.Any( char.IsControl )
			|| action is AdminAction.Ban or AdminAction.Mute && string.IsNullOrWhiteSpace( duration )
			|| action is not (AdminAction.Ban or AdminAction.Mute) && duration.Length != 0 )
		{
			throw new ArgumentException( "A printable reason (1–256 characters) and an explicit ban/mute duration are required." );
		}
		Action = action;
		SteamId = steamId;
		Reason = reason;
		Duration = duration;
	}

	internal CommandRequest ToCommand( Guid requestId ) => new( Action.ToString().ToLowerInvariant(),
		Action is AdminAction.Ban or AdminAction.Mute ? new[] { SteamId, Duration, Reason } : new[] { SteamId, Reason }, requestId );
}
