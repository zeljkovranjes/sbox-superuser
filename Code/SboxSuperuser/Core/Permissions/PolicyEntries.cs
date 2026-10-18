using System.Collections.ObjectModel;
using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser.Core.Permissions;

internal static class PolicyEntries
{
	internal static IReadOnlyDictionary<string, int> Snapshot( IEnumerable<KeyValuePair<string, int>> entries )
	{
		var seen = new HashSet<string>( StringComparer.Ordinal );
		var result = new Dictionary<string, int>( StringComparer.Ordinal );
		foreach ( var entry in entries ?? Array.Empty<KeyValuePair<string, int>>() )
		{
			ContractGuard.Key( entry.Key, nameof( entries ) );
			if ( !seen.Add( entry.Key ) || entry.Value is < 0 or > 2 )
			{
				throw new ArgumentException( "Permission entries must be unique and have an explicit valid effect.", nameof( entries ) );
			}

			// Unset is removal, never an explicit deny that would shadow inherited entries.
			if ( entry.Value != 0 )
			{
				result.Add( entry.Key, entry.Value );
			}
		}

		return new ReadOnlyDictionary<string, int>( result );
	}

	internal static IReadOnlyList<string> Groups( IEnumerable<string> groups )
	{
		var result = new List<string>();
		var seen = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var group in groups ?? Array.Empty<string>() )
		{
			ContractGuard.Key( group, nameof( groups ) );
			if ( group == "owner" || !seen.Add( group ) )
			{
				throw new ArgumentException( "Group references must be unique; owner membership is reserved.", nameof( groups ) );
			}

			result.Add( group );
		}

		return result.AsReadOnly();
	}
}
