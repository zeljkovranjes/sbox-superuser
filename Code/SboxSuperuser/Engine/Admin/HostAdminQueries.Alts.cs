using SboxSuperuser.Core.Alts;
using SboxSuperuser.Core.Moderation;

namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	internal const string AltsPermission = "admin.alts.read";
	/// <summary>Seeing raw IP addresses; owners only unless granted.</summary>
	internal const string AddressesPermission = "admin.alts.ips";
	private const int AltPageSize = 20;

	private AdminReply ReadAlts( Guid id, AdminQuery query, Func<string, bool> can )
	{
		if ( !can( AltsPermission ) ) return Failure( id, CommandStatus.Denied, $"Alt-account detection needs {AltsPermission}." );
		if ( _alts is null ) return Failure( id, CommandStatus.Unsupported, "Alt-account detection isn't running." );
		var online = _players()?.Select( player => player.SteamId ).ToHashSet() ?? new HashSet<string>();
		bool Banned( string steamId ) => _moderation.IsUsable && _moderation.CheckRestriction( steamId, ModerationKind.Ban ).Status == CommandStatus.Denied;
		string NameOf( string steamId ) => _alts.Ledger.Find( steamId )?.Names.FirstOrDefault() ?? steamId;
		if ( query.Kind == AdminQueryKind.Alts )
		{
			var links = _alts.Ledger.AltsOf( query.Search );
			return new AdminReply
			{
				RequestId = id, Status = CommandStatus.Success, Message = links.Count == 0 ? "No linked accounts." : $"{links.Count} linked accounts.",
				SteamId = query.Search,
				Addresses = can( AddressesPermission ) ? (_alts.Ledger.Find( query.Search )?.Addresses ?? new List<AltAddress>())
					.Select( address => new AdminAddressRow { Address = address.Address, FirstSeenUtc = address.FirstSeenUtc, LastSeenUtc = address.LastSeenUtc } ).ToArray()
					: Array.Empty<AdminAddressRow>(),
				Alts = links.Select( link => new AdminAltRow
				{
					SteamId = link.SteamId, Name = NameOf( link.SteamId ), Online = online.Contains( link.SteamId ), Banned = Banned( link.SteamId ),
					Evidence = link.Evidence.Select( Label ).ToArray(), LastSeenUtc = link.LastSeenUtc
				} ).ToArray()
			};
		}
		var suspects = _alts.Ledger.Suspects( Banned ).Skip( query.Page * AltPageSize ).Take( AltPageSize + 1 ).ToArray();
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "Suspected alt accounts.", HasMore = suspects.Length > AltPageSize,
			Alts = suspects.Take( AltPageSize ).Select( suspect => new AdminAltRow
			{
				SteamId = suspect.SteamId, Name = NameOf( suspect.SteamId ), Online = online.Contains( suspect.SteamId ), Banned = Banned( suspect.SteamId ),
				Score = suspect.Score, AltCount = suspect.Alts.Count, LinkedToBanned = suspect.Alts.Any( link => Banned( link.SteamId ) ),
				LastSeenUtc = _alts.Ledger.Find( suspect.SteamId )?.LastSeenUtc ?? default
			} ).ToArray()
		};
	}

	internal static string Label( AltEvidence evidence ) => evidence switch
	{
		AltEvidence.FamilyShare => "Family share",
		AltEvidence.SameNetwork => "Same IP",
		_ => "Same party"
	};
}
