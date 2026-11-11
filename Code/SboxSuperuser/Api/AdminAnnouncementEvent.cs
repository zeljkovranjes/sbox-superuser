namespace SboxSuperuser;

/// <summary>A public server announcement, separate from private command replies.</summary>
public sealed class AdminAnnouncementEvent
{
	public string Message { get; }
	public bool SuppressDefaultDisplay { get; set; }
	internal AdminAnnouncementEvent(string message) => Message = message;
}
