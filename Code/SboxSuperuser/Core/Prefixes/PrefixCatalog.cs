using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Prefixes;

/// <summary>Every prefix plus who wears which. Immutable: each change returns a new catalog.</summary>
internal sealed class PrefixCatalog
{
	internal const int MaximumPrefixes = 64;
	internal const int MaximumGroupAssignments = 128;
	internal const int MaximumUserAssignments = 1024;

	internal static readonly PrefixCatalog Empty = new( Array.Empty<PrefixDefinition>(),
		new Dictionary<string, string>( StringComparer.Ordinal ), new Dictionary<string, string>( StringComparer.Ordinal ) );

	internal PrefixCatalog( IEnumerable<PrefixDefinition> prefixes, IReadOnlyDictionary<string, string> groups, IReadOnlyDictionary<string, string> users )
	{
		Prefixes = Array.AsReadOnly( prefixes.OrderBy( prefix => prefix.Id, StringComparer.Ordinal ).ToArray() );
		Groups = groups;
		Users = users;
		if ( Prefixes.Count > MaximumPrefixes ) throw new ArgumentException( $"A server can have at most {MaximumPrefixes} prefixes." );
		if ( Groups.Count > MaximumGroupAssignments || Users.Count > MaximumUserAssignments )
			throw new ArgumentException( "Too many prefix assignments." );
		if ( Prefixes.Select( prefix => prefix.Id ).Distinct( StringComparer.Ordinal ).Count() != Prefixes.Count )
			throw new ArgumentException( "Prefix IDs must be unique." );
		if ( Groups.Values.Concat( Users.Values ).Any( id => Find( id ) is null ) )
			throw new ArgumentException( "An assignment names a prefix that does not exist." );
	}

	internal IReadOnlyList<PrefixDefinition> Prefixes { get; }

	/// <summary>Group ID to prefix ID.</summary>
	internal IReadOnlyDictionary<string, string> Groups { get; }

	/// <summary>Steam ID to prefix ID; a player's own prefix beats every group prefix.</summary>
	internal IReadOnlyDictionary<string, string> Users { get; }

	internal PrefixDefinition Find( string id ) => Prefixes.FirstOrDefault( prefix => prefix.Id == id );

	/// <summary>The player's own prefix, else the prefix of their highest-protection group (ties by group ID), else null.</summary>
	internal PrefixDefinition Resolve( string steamId, IEnumerable<GroupRank> groups )
	{
		if ( steamId is not null && Users.TryGetValue( steamId, out var own ) ) return Find( own );
		var best = groups
			.Where( group => Groups.ContainsKey( group.GroupId ) )
			.OrderByDescending( group => group.Protection )
			.ThenBy( group => group.GroupId, StringComparer.Ordinal )
			.FirstOrDefault();
		return best.GroupId is null ? null : Find( Groups[ best.GroupId ] );
	}

	internal PrefixCatalog Save( PrefixDefinition prefix ) =>
		new( Prefixes.Where( existing => existing.Id != prefix.Id ).Append( prefix ), Groups, Users );

	/// <summary>Deletes a prefix and every assignment that used it.</summary>
	internal PrefixCatalog Delete( string id ) => new( Prefixes.Where( prefix => prefix.Id != id ),
		Without( Groups, pair => pair.Value == id ), Without( Users, pair => pair.Value == id ) );

	internal PrefixCatalog AssignGroup( string groupId, string prefixId )
	{
		ContractGuard.Key( groupId, nameof( groupId ) );
		return new( Prefixes, Assign( Groups, groupId, prefixId ), Users );
	}

	internal PrefixCatalog AssignUser( string steamId, string prefixId )
	{
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		return new( Prefixes, Groups, Assign( Users, steamId, prefixId ) );
	}

	// A null prefix ID clears the assignment.
	private static IReadOnlyDictionary<string, string> Assign( IReadOnlyDictionary<string, string> source, string key, string prefixId )
	{
		var copy = new Dictionary<string, string>( source.Where( pair => pair.Key != key ), StringComparer.Ordinal );
		if ( prefixId is not null ) copy[ key ] = prefixId;
		return copy;
	}

	private static IReadOnlyDictionary<string, string> Without( IReadOnlyDictionary<string, string> source, Func<KeyValuePair<string, string>, bool> remove ) =>
		new Dictionary<string, string>( source.Where( pair => !remove( pair ) ), StringComparer.Ordinal );
}
