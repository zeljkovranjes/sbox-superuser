namespace SboxSuperuser.Core.Prefixes;

/// <summary>
/// What clients receive: every prefix plus the prefix ID of each online player. Group membership never leaves
/// the host. Disabled means the module is off and every name draws plain.
/// </summary>
internal sealed record PrefixSnapshot( bool Enabled, bool Chat, IReadOnlyList<PrefixDefinition> Prefixes, IReadOnlyDictionary<string, string> Players )
{
	internal static readonly PrefixSnapshot Disabled = new( false, false, Array.Empty<PrefixDefinition>(),
		new Dictionary<string, string>( StringComparer.Ordinal ) );

	internal PrefixDefinition Find( string id ) => id is null ? null : Prefixes.FirstOrDefault( prefix => prefix.Id == id );

	internal PrefixDefinition For( string steamId ) =>
		Enabled && steamId is not null && Players.TryGetValue( steamId, out var id ) ? Find( id ) : null;
}
