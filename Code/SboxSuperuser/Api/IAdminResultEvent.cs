using Sandbox;

namespace SboxSuperuser;

/// <summary>Receives locally requested private admin replies, including timeout and disconnect outcomes.</summary>
public interface IAdminResultEvent : ISceneEvent<IAdminResultEvent>
{
	/// <summary>Handles a correlated reply on the local scene; no ImGui dependency is required.</summary>
	void OnAdminResult( AdminReply reply );
}
