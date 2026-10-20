using Sandbox;

namespace SboxSuperuser;

/// <summary>Implement on a component to hear when a player's prefix changes on this machine.</summary>
public interface IPrefixChangedEvent : ISceneEvent<IPrefixChangedEvent>
{
	/// <summary>Called with the player's new prefix, or null when they no longer have one.</summary>
	void OnPrefixChanged( string steamId, PlayerPrefix prefix );
}
