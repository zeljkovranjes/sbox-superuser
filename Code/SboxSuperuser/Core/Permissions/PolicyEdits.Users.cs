using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Permissions;

internal static partial class PolicyEdits
{
	private static bool ChangeUser( Dictionary<string, UserDefinition> users,
		Dictionary<string, GroupDefinition> groups, PolicyEdit edit )
	{
		SteamIdentity.Validate( edit.Subject, nameof( edit.Subject ) );
		var user = users.TryGetValue( edit.Subject, out var stored ) ? stored : new UserDefinition( edit.Subject, null, null );
		var assignments = user.Groups;
		var entries = user.Entries;
		if ( edit.Kind == PolicyEditKind.SetUserPermission )
		{
			entries = ChangeEntry( entries, edit.Value, edit.Effect );
			if ( ReferenceEquals( entries, user.Entries ) )
			{
				return false;
			}
		}
		else
		{
			ValidateGroupId( edit.Value );
			if ( !groups.ContainsKey( edit.Value ) )
			{
				throw new ArgumentException( "Assigned group does not exist." );
			}

			var add = edit.Kind == PolicyEditKind.AddUserGroup;
			if ( assignments.Contains( edit.Value ) == add )
			{
				return false;
			}

			assignments = Array.AsReadOnly( (add ? assignments.Append( edit.Value ) : assignments.Where( id => id != edit.Value )).ToArray() );
		}

		users[ user.SteamId ] = new UserDefinition( user.SteamId, assignments, entries );
		return true;
	}
}
