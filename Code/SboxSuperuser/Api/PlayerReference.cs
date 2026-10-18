using SboxSuperuser.Core.Identity;

namespace SboxSuperuser;

/// <summary>A resolved stable player ID; it promises neither an online connection nor target authorization.</summary>
public sealed class PlayerReference
{
	/// <summary>Gets the canonical decimal Steam ID, including for offline targets.</summary>
	public string SteamId { get; }

	/// <summary>Creates a stable reference; authentication and target rechecks remain separate.</summary>
	public PlayerReference( string steamId )
	{
		SteamId = SteamIdentity.Validate( steamId, nameof( steamId ) );
	}
}
