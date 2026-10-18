using Sandbox;

namespace SuperuserCompatibilityLab;

/// <summary>Local presentation event for the independent example backend's ordinary messages.</summary>
public interface IExampleChatMessageEvent : ISceneEvent<IExampleChatMessageEvent>
{
	/// <summary>Displays text already authenticated and routed by the example host.</summary>
	void OnExampleChatMessage( string name, string message );
}
