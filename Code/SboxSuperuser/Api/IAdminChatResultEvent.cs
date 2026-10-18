using Sandbox;

namespace SboxSuperuser;

/// <summary>Implement on a client scene component to render authenticated private command replies.</summary>
public interface IAdminChatResultEvent : ISceneEvent<IAdminChatResultEvent>
{
	/// <summary>Receives one host reply locally; disabled or removed scene components are not subscribed.</summary>
	void OnAdminChatResult( AdminChatResultEvent message );
}
