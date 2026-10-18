using Sandbox;

namespace SboxSuperuser.UI;

/// <summary>Outcome colors drawn over the stock Dear ImGui dark style.</summary>
internal static class AdminMenuTheme
{
	internal static readonly Vector4 Success = Rgb( 0x4ADE80 );
	internal static readonly Vector4 Warning = Rgb( 0xFBBF24 );
	internal static readonly Vector4 Danger = Rgb( 0xF87171 );
	internal static readonly Vector4 Neutral = Rgb( 0x9CA3AF );

	/// <summary>Maps a host outcome to its color; Unknown is a warning because it is never success.</summary>
	internal static Vector4 Tone( CommandStatus status ) => status switch
	{
		CommandStatus.Success => Success,
		CommandStatus.NoChange => Neutral,
		CommandStatus.Unknown or CommandStatus.Cancelled or CommandStatus.Conflict => Warning,
		_ => Danger
	};

	internal static Vector4 Tone( AdminAction action ) => action switch
	{
		AdminAction.Kick or AdminAction.Ban => Danger,
		AdminAction.Warn or AdminAction.Mute => Warning,
		_ => Success
	};

	private static Vector4 Rgb( int hex ) => new( ((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, 1 );
}
