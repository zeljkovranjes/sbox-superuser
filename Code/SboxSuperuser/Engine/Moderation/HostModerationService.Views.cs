using SboxSuperuser.Core.Moderation;

namespace SboxSuperuser.Engine.Moderation;

internal sealed partial class HostModerationService
{
	// HostAdminQueries applies permissions before copying any private records.
	internal AdminModerationRow[] ReadView( string target, bool history, int page, bool bans, bool mutes, out bool more )
	{
		var now = _clock();
		bool Active( ModerationEntry entry ) => entry.Kind is ModerationKind.Ban or ModerationKind.TextMute
			&& _ledger.Active( target, entry.Kind, now )?.Id == entry.Id;
		var entries = _ledger.Entries.Where( entry => entry.Target == target && (history
			|| Active( entry ) && (entry.Kind == ModerationKind.Ban && bans || entry.Kind == ModerationKind.TextMute && mutes)) )
			.Reverse().Skip( page * 20 ).Take( 21 ).ToArray();
		more = entries.Length > 20;
		return entries.Take( 20 ).Select( entry => new AdminModerationRow
		{
			Kind = entry.Kind.ToString(), RequestId = entry.RequestId, CreatedUtc = entry.CreatedUtc,
			ExpiresUtc = entry.ExpiresUtc, Reason = entry.Reason, Active = Active( entry )
		} ).ToArray();
	}
}
