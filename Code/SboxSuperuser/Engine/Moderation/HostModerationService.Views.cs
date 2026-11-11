using SboxSuperuser.Core.Moderation;

namespace SboxSuperuser.Engine.Moderation;

internal sealed partial class HostModerationService
{
	// HostAdminQueries applies permissions before copying any private records.
	internal AdminModerationRow[] ReadView( string target, bool history, int page, Func<ModerationKind, bool> visible, out bool more )
	{
		var now = _clock();
		bool Active( ModerationEntry entry ) => entry.Kind.IsRestriction() && _ledger.Active( target, entry.Kind, now )?.Id == entry.Id;
		var entries = _ledger.Entries.Where( entry => entry.Target == target && (history || Active( entry ) && visible( entry.Kind )) )
			.Reverse().Skip( page * 20 ).Take( 21 ).ToArray();
		more = entries.Length > 20;
		return entries.Take( 20 ).Select( entry => new AdminModerationRow
		{
			Kind = entry.Kind.ToString(), RequestId = entry.RequestId, CreatedUtc = entry.CreatedUtc,
			ExpiresUtc = entry.ExpiresUtc, Actor = entry.ActorSubject, Reason = entry.Reason, Active = Active( entry )
		} ).ToArray();
	}

	/// <summary>Active visible restrictions across every player, newest first; search matches the Steam ID.</summary>
	internal AdminModerationRow[] ReadActive( Func<ModerationKind, bool> visible, string search, int page, out bool more )
	{
		var entries = ReadAllActive( visible, search ).Skip( page * 20 ).Take( 21 ).ToArray();
		more = entries.Length > 20;
		return entries.Take( 20 ).ToArray();
	}

	internal AdminModerationRow[] ReadAllActive( Func<ModerationKind, bool> visible, string search )
	{
		var now = _clock();
		return _ledger.Entries
			.Where( entry => entry.Kind.IsRestriction() && visible( entry.Kind ) )
			.Where( entry => entry.Target.Contains( search, StringComparison.Ordinal ) )
			.Where( entry => _ledger.Active( entry.Target, entry.Kind, now )?.Id == entry.Id )
			.Reverse().Select( entry => new AdminModerationRow
		{
			SteamId = entry.Target, Kind = entry.Kind.ToString(), RequestId = entry.RequestId, CreatedUtc = entry.CreatedUtc,
			ExpiresUtc = entry.ExpiresUtc, Actor = entry.ActorSubject, Reason = entry.Reason, Active = true
		} ).ToArray();
	}

	internal int CountWarnings( string target ) =>
		_ledger.Entries.Count( entry => entry.Target == target && entry.Kind == ModerationKind.Warn );
}
