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
	Unmute,
	/// <summary>Silence voice chat for an explicit duration or permanently.</summary>
	Gag,
	/// <summary>Revoke a voice restriction.</summary>
	Ungag,
	/// <summary>Replace the player's chat messages with silly lines for an explicit duration or permanently.</summary>
	Gimp,
	/// <summary>Revoke a gimp.</summary>
	Ungimp
}

internal static class AdminActions
{
	/// <summary>Timed actions take a duration argument before the reason.</summary>
	internal static bool IsTimed( this AdminAction action ) => action is AdminAction.Ban or AdminAction.Mute or AdminAction.Gag or AdminAction.Gimp;

	internal static bool IsRevocation( this AdminAction action ) => action is AdminAction.Unban or AdminAction.Unmute or AdminAction.Ungag or AdminAction.Ungimp;
}
