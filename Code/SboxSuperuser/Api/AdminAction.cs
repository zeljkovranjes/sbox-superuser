namespace SboxSuperuser;

/// <summary>Moderation operations available to replacement interfaces; each uses its existing command permission.</summary>
public enum AdminAction
{
	/// <summary>Disconnect an online player.</summary>
	Kick,
	/// <summary>Record a warning and notify an online player.</summary>
	Warn,
	/// <summary>Ban a Steam ID for an explicit duration or permanently.</summary>
	Ban,
	/// <summary>Revoke an active ban.</summary>
	Unban,
	/// <summary>Restrict ordinary text chat.</summary>
	Mute,
	/// <summary>Revoke a text restriction.</summary>
	Unmute
}
