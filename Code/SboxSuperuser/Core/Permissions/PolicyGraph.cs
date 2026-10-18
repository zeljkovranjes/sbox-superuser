using System.Collections.ObjectModel;
using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser.Core.Permissions;

internal sealed class PolicyGraph
{
	private readonly Dictionary<string, IReadOnlyList<string>> _ancestors = new( StringComparer.Ordinal );
	internal IReadOnlyDictionary<string, GroupDefinition> Groups { get; }
	internal IReadOnlyDictionary<string, UserDefinition> Users { get; }
	internal long Revision { get; }

	internal PolicyGraph( long revision, IEnumerable<GroupDefinition> groups, IEnumerable<UserDefinition> users )
	{
		if ( revision < 0 )
		{
			throw new ArgumentOutOfRangeException( nameof( revision ) );
		}

		Revision = revision;
		var groupCopy = ContractGuard.Copy( groups, nameof( groups ) );
		var userCopy = ContractGuard.Copy( users, nameof( users ) );
		if ( groupCopy.Count is < 1 or > 256 || userCopy.Count > 10000 )
		{
			throw new ArgumentException( "Policy supports 1–256 groups and up to 10000 stored users." );
		}

		Groups = new ReadOnlyDictionary<string, GroupDefinition>( groupCopy.ToDictionary( group => group.Id, StringComparer.Ordinal ) );
		Users = new ReadOnlyDictionary<string, UserDefinition>( userCopy.ToDictionary( user => user.SteamId, StringComparer.Ordinal ) );
		if ( !Groups.ContainsKey( "player" ) )
		{
			throw new ArgumentException( "The implicit player group is required.", nameof( groups ) );
		}

		foreach ( var group in Groups.Values )
		{
			ValidateReferences( group.Parents );
		}

		foreach ( var user in Users.Values )
		{
			ValidateReferences( user.Groups );
		}

		foreach ( var id in Groups.Keys )
		{
			BuildAncestors( id, new HashSet<string>( StringComparer.Ordinal ) );
		}
	}

	internal IReadOnlyList<string> ApplicableGroups( string steamId )
	{
		var result = new HashSet<string>( _ancestors[ "player" ], StringComparer.Ordinal );
		if ( Users.TryGetValue( steamId, out var user ) )
		{
			foreach ( var id in user.Groups )
			{
				result.UnionWith( _ancestors[ id ] );
			}
		}

		return Array.AsReadOnly( result.OrderBy( id => id, StringComparer.Ordinal ).ToArray() );
	}

	private void ValidateReferences( IEnumerable<string> references )
	{
		foreach ( var id in references )
		{
			if ( !Groups.ContainsKey( id ) )
			{
				throw new ArgumentException( $"Referenced group '{id}' does not exist." );
			}
		}
	}

	private IReadOnlyList<string> BuildAncestors( string id, HashSet<string> visiting )
	{
		if ( _ancestors.TryGetValue( id, out var cached ) )
		{
			return cached;
		}

		if ( !visiting.Add( id ) )
		{
			throw new ArgumentException( $"Group inheritance cycle includes '{id}'." );
		}

		var result = new HashSet<string>( StringComparer.Ordinal ) { id };
		foreach ( var parent in Groups[ id ].Parents )
		{
			result.UnionWith( BuildAncestors( parent, visiting ) );
		}

		visiting.Remove( id );
		var snapshot = Array.AsReadOnly( result.OrderBy( group => group, StringComparer.Ordinal ).ToArray() );
		_ancestors.Add( id, snapshot );
		return snapshot;
	}
}
