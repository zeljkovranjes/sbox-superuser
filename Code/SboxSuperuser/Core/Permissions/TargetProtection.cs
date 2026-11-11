using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Permissions;

internal static class TargetProtection
{
	internal const int OwnerLevel = 100;
	internal const int MaximumGroupLevel = OwnerLevel - 1;

	internal static int Level( PolicyGraph policy, OwnerSet owners, string steamId, ClaimMemberships claims = null )
	{
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		var groups = new HashSet<string>( policy.ApplicableGroups( steamId, claims ), StringComparer.Ordinal );
		var level = 0;
		if ( owners.Contains( steamId ) || claims?.IsOwner( steamId ) == true )
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
		string actorSteamId, bool dedicatedConsole, string targetSteamId, ClaimMemberships claims = null )
	{
		var targetLevel = Level( policy, owners, targetSteamId, claims );
		if ( dedicatedConsole )
		{
			return new ProtectionDecision( true, int.MaxValue, targetLevel, "Dedicated local console may target anyone." );
		}

		var actorLevel = Level( policy, owners, actorSteamId, claims );
		if ( actorSteamId == targetSteamId )
		{
			return new ProtectionDecision( false, actorLevel, targetLevel, "Punitive self-targeting is refused." );
		}

		return new ProtectionDecision( actorLevel > targetLevel, actorLevel, targetLevel,
			actorLevel > targetLevel ? "Actor protection exceeds target protection." : "Actor protection must strictly exceed target protection." );
	}
}
