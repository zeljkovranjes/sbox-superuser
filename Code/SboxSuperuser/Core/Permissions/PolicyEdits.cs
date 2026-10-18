using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser.Core.Permissions;

internal static partial class PolicyEdits
{
	internal static PolicyGraph Apply( PolicyGraph policy, PolicyEdit edit )
	{
		ArgumentNullException.ThrowIfNull( policy );
		ArgumentNullException.ThrowIfNull( edit );
		if ( policy.Revision == long.MaxValue )
		{
			throw new ArgumentException( "Policy revision is exhausted; explicit recovery is required." );
		}

		var groups = policy.Groups.ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		var users = policy.Users.ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		var changed = edit.Kind switch
		{
			PolicyEditKind.CreateGroup or PolicyEditKind.DeleteGroup or PolicyEditKind.SetParents
				or PolicyEditKind.SetGroupPermission or PolicyEditKind.SetGroupProtection => ChangeGroup( groups, users, edit ),
			PolicyEditKind.AddUserGroup or PolicyEditKind.RemoveUserGroup or PolicyEditKind.SetUserPermission => ChangeUser( users, groups, edit ),
			_ => throw new ArgumentException( "Unknown policy operation.", nameof( edit ) )
		};

		// Complete graph validation happens before a candidate can be persisted or activated.
		return changed ? new PolicyGraph( policy.Revision + 1, groups.Values, users.Values ) : policy;
	}

	private static IReadOnlyDictionary<string, int> ChangeEntry( IReadOnlyDictionary<string, int> entries, string key, int effect )
	{
		ContractGuard.Key( key, nameof( key ) );
		if ( effect is < 0 or > 2 )
		{
			throw new ArgumentException( "Invalid permission effect.", nameof( effect ) );
		}

		entries.TryGetValue( key, out var current );
		if ( current == effect )
		{
			return entries;
		}

		var copy = entries.ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		if ( effect == 0 )
		{
			copy.Remove( key );
		}
		else
		{
			copy[ key ] = effect;
		}

		return copy;
	}

	private static void ValidateGroupId( string id )
	{
		ContractGuard.Key( id, nameof( id ) );
		if ( id == "owner" )
		{
			throw new ArgumentException( "Owner is reserved and cannot be managed as an ordinary group." );
		}
	}
}
