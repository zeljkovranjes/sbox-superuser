using Sandbox;
namespace SboxSuperuser;

/// <summary>Receives verified host announcements for local presentation.</summary>
public interface IAdminAnnouncementEvent : ISceneEvent<IAdminAnnouncementEvent>
{
	void OnAdminAnnouncement(AdminAnnouncementEvent announcement);
}
