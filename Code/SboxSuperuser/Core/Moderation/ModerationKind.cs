namespace SboxSuperuser.Core.Moderation;

// Stored by name, so existing members keep their names; new kinds go at the end.
internal enum ModerationKind
{
	Kick,
	Ban,
	Warn,
	TextMute,
	Unban,
	Unmute,
	Note,
	Gag,
	Ungag,
	Gimp,
	Ungimp
}

/// <summary>Which kinds are timed restrictions and which revoke them; the one place these pairs are defined.</summary>
internal static class ModerationKinds
{
	/// <summary>Restrictions stay active until they expire or are revoked.</summary>
	internal static bool IsRestriction( this ModerationKind kind ) => kind is ModerationKind.Ban or ModerationKind.TextMute or ModerationKind.Gag or ModerationKind.Gimp;

	internal static bool IsRevocation( this ModerationKind kind ) => kind is ModerationKind.Unban or ModerationKind.Unmute or ModerationKind.Ungag or ModerationKind.Ungimp;

	/// <summary>The restriction a revocation lifts; any other kind maps to itself.</summary>
	internal static ModerationKind Restricted( this ModerationKind kind ) => kind switch
	{
		ModerationKind.Unban => ModerationKind.Ban,
		ModerationKind.Unmute => ModerationKind.TextMute,
		ModerationKind.Ungag => ModerationKind.Gag,
		ModerationKind.Ungimp => ModerationKind.Gimp,
		_ => kind
	};

	/// <summary>Kinds that act against a player and so respect target protection.</summary>
	internal static bool IsPunitive( this ModerationKind kind ) => kind is ModerationKind.Kick or ModerationKind.Warn || kind.IsRestriction();
}
