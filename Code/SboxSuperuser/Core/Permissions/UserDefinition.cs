using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Permissions;

internal sealed class UserDefinition
{
	internal string SteamId { get; }
	internal IReadOnlyList<string> Groups { get; }
	internal IReadOnlyDictionary<string, int> Entries { get; }

	internal UserDefinition( string steamId, IEnumerable<string> groups,
		IEnumerable<KeyValuePair<string, int>> entries )
	{
		SteamId = SteamIdentity.Validate( steamId, nameof( steamId ) );
		Groups = PolicyEntries.Groups( groups );
		Entries = PolicyEntries.Snapshot( entries );
	}
}
