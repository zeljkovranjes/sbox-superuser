namespace SboxSuperuser.Core.Permissions;

internal static class PolicyResolution
{
	internal static PolicyDecision Evaluate( PolicyGraph graph, string steamId, string permission,
		bool registered, long registryRevision )
	{
		var direct = 0;
		if ( graph.Users.TryGetValue( steamId, out var user ) )
		{
			user.Entries.TryGetValue( permission, out direct );
		}

		var contributions = new List<PolicyContribution>();
		foreach ( var id in graph.ApplicableGroups( steamId ) )
		{
			if ( graph.Groups[ id ].Entries.TryGetValue( permission, out var effect ) )
			{
				contributions.Add( new PolicyContribution( id, effect ) );
			}
		}

		var allowed = false;
		string reason;
		if ( !registered )
		{
			reason = "Permission is not registered; access denied.";
		}
		else if ( direct != 0 )
		{
			allowed = direct == 2;
			reason = allowed ? "Direct user allow overrides inherited entries." : "Direct user deny overrides inherited entries.";
		}
		else if ( contributions.Any( entry => entry.Effect == 1 ) )
		{
			reason = "Inherited deny takes precedence over inherited allow.";
		}
		else if ( contributions.Any( entry => entry.Effect == 2 ) )
		{
			allowed = true;
			reason = "Inherited allow with no inherited deny.";
		}
		else
		{
			reason = "No matching permission entry; access denied.";
		}

		return new PolicyDecision( permission, allowed, direct, contributions.AsReadOnly(),
			graph.Revision, registryRevision, registered, reason );
	}
}
