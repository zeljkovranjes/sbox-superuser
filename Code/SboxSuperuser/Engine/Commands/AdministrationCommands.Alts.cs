using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Alts;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	private void RegisterAlts( HostAltService alts )
	{
		Register( "alts.check", "List the accounts linked to a player and why", HostAdminQueries.AltsPermission, new[] { Player() },
			(_, arguments) =>
			{
				var target = arguments.Get<PlayerReference>( "user" ).SteamId;
				var links = alts.Ledger.AltsOf( target );
				return Task.FromResult( Rows( links.Select( link => $"{link.SteamId} ({alts.Ledger.Find( link.SteamId )?.Names.FirstOrDefault() ?? "never joined"}): "
					+ string.Join( ", ", link.Evidence.Select( HostAdminQueries.Label ) ) ), 1, links.Count == 0 ? "No linked accounts." : $"{links.Count} linked accounts." ) );
			} );
		Register( "alts.ips", "List the IP addresses a player connected from, newest first", HostAdminQueries.AddressesPermission, new[] { Player() },
			(_, arguments) =>
			{
				var addresses = alts.Ledger.Find( arguments.Get<PlayerReference>( "user" ).SteamId )?.Addresses ?? new List<Core.Alts.AltAddress>();
				return Task.FromResult( Rows( addresses.Select( address => $"{address.Address}: first {address.FirstSeenUtc:yyyy-MM-dd HH:mm}, last {address.LastSeenUtc:yyyy-MM-dd HH:mm} UTC" ),
					1, addresses.Count == 0 ? "No IP addresses recorded; relayed connections don't reveal one." : $"{addresses.Count} IP addresses." ) );
			} );
		Register( "alts.suspects", "List accounts linked to others, riskiest first", HostAdminQueries.AltsPermission, new[] { Page() },
			(_, arguments) => Task.FromResult( Rows( alts.Ledger.Suspects( steamId => _moderation?.IsUsable == true
					&& _moderation.CheckRestriction( steamId, Core.Moderation.ModerationKind.Ban ).Status == CommandStatus.Denied )
				.Select( suspect => $"{suspect.SteamId} ({alts.Ledger.Find( suspect.SteamId )?.Names.FirstOrDefault()}): {suspect.Alts.Count} linked, score {suspect.Score}" ),
				PageNumber( arguments ), "Suspected alt accounts." ) ) );
	}
}
