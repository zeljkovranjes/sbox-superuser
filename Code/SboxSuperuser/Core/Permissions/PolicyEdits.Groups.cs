namespace SboxSuperuser.Core.Permissions;

internal static partial class PolicyEdits
{
	private static bool ChangeGroup( Dictionary<string, GroupDefinition> groups,
		Dictionary<string, UserDefinition> users, PolicyEdit edit )
	{
		ValidateGroupId( edit.Subject );
		if ( edit.Kind == PolicyEditKind.CreateGroup )
		{
			if ( groups.ContainsKey( edit.Subject ) )
			{
				throw new ArgumentException( "Group already exists." );
			}

			groups.Add( edit.Subject, new GroupDefinition( edit.Subject, null, null, 0 ) );
			return true;
		}

		if ( !groups.TryGetValue( edit.Subject, out var group ) )
		{
			throw new ArgumentException( "Group does not exist." );
		}

		if ( edit.Kind == PolicyEditKind.DeleteGroup )
		{
			if ( edit.Subject == "player" || groups.Values.Any( item => item.Parents.Contains( edit.Subject ) )
				|| users.Values.Any( item => item.Groups.Contains( edit.Subject ) ) )
			{
				throw new ArgumentException( "Cannot delete player or a referenced group; remove explicit references first." );
			}

			groups.Remove( edit.Subject );
			return true;
		}

		var parents = group.Parents;
		var entries = group.Entries;
		var protection = group.ProtectionLevel;
		switch ( edit.Kind )
		{
			case PolicyEditKind.SetParents:
				parents = PolicyEntries.Groups( edit.Parents );
				if ( new HashSet<string>( parents, StringComparer.Ordinal ).SetEquals( group.Parents ) )
				{
					return false;
				}
				break;
			case PolicyEditKind.SetGroupPermission:
				entries = ChangeEntry( group.Entries, edit.Value, edit.Effect );
				if ( ReferenceEquals( entries, group.Entries ) )
				{
					return false;
				}
				break;
			case PolicyEditKind.SetGroupProtection:
				protection = edit.ProtectionLevel;
				if ( protection == group.ProtectionLevel )
				{
					return false;
				}
				break;
		}

		groups[ group.Id ] = new GroupDefinition( group.Id, parents, entries, protection );
		return true;
	}
}
