using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Permissions;

internal static class TargetProtection
{
	internal const int OwnerLevel = 100;
	internal const int MaximumGroupLevel = OwnerLevel - 1;

	internal static int Level( PolicyGraph policy, OwnerSet owners, string steamId )
	{
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		var groups = new HashSet<string>( policy.ApplicableGroups( steamId ), StringComparer.Ordinal );
		var level = 0;
		if ( owners.Contains( steamId ) )
		{
			level = OwnerLevel;
			if ( policy.Groups.ContainsKey( "admin" ) )
			{
				groups.UnionWith( policy.AncestorsOf( "admin" ) );
			}
		}

		foreach ( var group in groups )
		{
			level = Math.Max( level, policy.Groups[ group ].ProtectionLevel );
		}

		return level;
	}

	internal static ProtectionDecision Check( PolicyGraph policy, OwnerSet owners,
		string actorSteamId, bool dedicatedConsole, string targetSteamId )
	{
		var targetLevel = Level( policy, owners, targetSteamId );
		if ( dedicatedConsole )
		{
			return new ProtectionDecision( true, int.MaxValue, targetLevel, "Dedicated local console may target anyone." );
		}

		var actorLevel = Level( policy, owners, actorSteamId );
		if ( actorSteamId == targetSteamId )
		{
			return new ProtectionDecision( false, actorLevel, targetLevel, "Punitive self-targeting is refused." );
		}

		return new ProtectionDecision( actorLevel > targetLevel, actorLevel, targetLevel,
			actorLevel > targetLevel ? "Actor protection exceeds target protection." : "Actor protection must strictly exceed target protection." );
	}
}
