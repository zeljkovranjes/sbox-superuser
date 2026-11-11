namespace SboxSuperuser.Core.Props;

/// <summary>
/// Who owns each spawned object and who they share with. Ownership is remembered after the owner leaves, so their
/// props stay protected until cleaned up. Objects with no recorded owner belong to the map.
/// </summary>
internal sealed class PropOwnership
{
	private readonly Dictionary<Guid, string> _owners = new();
	private readonly Dictionary<string, HashSet<string>> _shared = new( StringComparer.Ordinal );

	internal int Count => _owners.Count;

	/// <summary>Records the object's owner the first time it is seen; later owners don't replace the first.</summary>
	internal void Record( Guid objectId, string ownerSteamId )
	{
		if ( !string.IsNullOrEmpty( ownerSteamId ) ) _owners.TryAdd( objectId, ownerSteamId );
	}

	internal void Forget( Guid objectId ) => _owners.Remove( objectId );

	internal string OwnerOf( Guid objectId ) => _owners.TryGetValue( objectId, out var owner ) ? owner : null;

	internal IReadOnlyList<Guid> ObjectsOf( string ownerSteamId ) =>
		_owners.Where( pair => pair.Value == ownerSteamId ).Select( pair => pair.Key ).ToArray();

	internal IReadOnlyList<Guid> All => _owners.Keys.ToArray();

	/// <summary>Lets the friend touch everything the owner spawns; false when they already could.</summary>
	internal bool Share( string ownerSteamId, string friendSteamId )
	{
		if ( ownerSteamId == friendSteamId ) return false;
		if ( !_shared.TryGetValue( ownerSteamId, out var friends ) ) _shared[ ownerSteamId ] = friends = new HashSet<string>( StringComparer.Ordinal );
		return friends.Add( friendSteamId );
	}

	internal bool Unshare( string ownerSteamId, string friendSteamId ) =>
		_shared.TryGetValue( ownerSteamId, out var friends ) && friends.Remove( friendSteamId );

	/// <summary>
	/// Whether the actor may pick up, move, tool or delete the object: its owner and their friends may, staff with
	/// the override may touch anything, and map objects are for staff only.
	/// </summary>
	internal bool CanTouch( string actorSteamId, Guid objectId, bool staffOverride )
	{
		if ( staffOverride ) return true;
		var owner = OwnerOf( objectId );
		if ( owner is null ) return false;
		return owner == actorSteamId || _shared.TryGetValue( owner, out var friends ) && friends.Contains( actorSteamId );
	}
}
