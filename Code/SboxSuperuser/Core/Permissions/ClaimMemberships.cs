using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Permissions;

/// <summary>
/// Access granted by the engine's config/users.json claims to connected players. It is live, never saved: removing
/// someone from users.json removes the access the next time they connect, and leaving removes it at once.
/// </summary>
internal sealed class ClaimMemberships
{
	/// <summary>Each claim Superuser understands and what it grants; null means owner.</summary>
	internal static readonly IReadOnlyDictionary<string, string> Grants = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase )
	{
		[ "superuser" ] = null, [ "owner" ] = null, [ "admin" ] = "admin", [ "moderator" ] = "moderator", [ "mod" ] = "moderator"
	};

	private readonly Dictionary<string, (bool Owner, string[] Groups)> _players = new( StringComparer.Ordinal );

	/// <summary>Raised whenever any player's claimed access changes, so cached decisions can be dropped.</summary>
	internal event Action Changed;

	/// <summary>Records what a player's claims grant; claims Superuser doesn't know are ignored.</summary>
	internal void Set( string steamId, IEnumerable<string> claims )
	{
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		var owner = false;
		var groups = new SortedSet<string>( StringComparer.Ordinal );
		foreach ( var claim in claims )
		{
			if ( !Grants.TryGetValue( claim, out var group ) ) continue;
			if ( group is null ) owner = true;
			else groups.Add( group );
		}
		if ( !owner && groups.Count == 0 ) { Remove( steamId ); return; }
		_players[ steamId ] = (owner, groups.ToArray());
		Changed?.Invoke();
	}

	internal void Remove( string steamId )
	{
		if ( _players.Remove( steamId ) ) Changed?.Invoke();
	}

	internal bool IsOwner( string steamId ) => _players.TryGetValue( steamId, out var access ) && access.Owner;

	internal IReadOnlyList<string> GroupsOf( string steamId ) =>
		_players.TryGetValue( steamId, out var access ) ? access.Groups : Array.Empty<string>();
}
