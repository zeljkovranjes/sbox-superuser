using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Permissions;

internal sealed class OwnerSet
{
	private readonly HashSet<string> _ids;
	internal IReadOnlyList<string> Ids { get; }

	internal OwnerSet( IEnumerable<string> ids )
	{
		ArgumentNullException.ThrowIfNull( ids );
		_ids = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var id in ids )
		{
			SteamIdentity.Validate( id, nameof( ids ) );
			if ( !_ids.Add( id ) || _ids.Count > 128 )
			{
				throw new ArgumentException( "Owner IDs must be unique; at most 128 are supported.", nameof( ids ) );
			}
		}

		Ids = Array.AsReadOnly( _ids.OrderBy( id => id, StringComparer.Ordinal ).ToArray() );
	}

	internal bool Contains( string steamId ) => _ids.Contains( steamId );

	internal OwnerSet Change( string steamId, bool add )
	{
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		if ( _ids.Contains( steamId ) == add )
		{
			return this;
		}

		if ( !add && _ids.Count == 1 )
		{
			throw new InvalidOperationException( "Cannot remove the final owner; explicit dedicated-local recovery is required." );
		}

		return new OwnerSet( add ? Ids.Append( steamId ) : Ids.Where( id => id != steamId ) );
	}
}
