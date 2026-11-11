namespace SboxSuperuser.Core.Alts;

/// <summary>How strongly two accounts are tied together.</summary>
internal enum AltEvidence
{
	/// <summary>Joined in the same Steam party; usually friends, sometimes one person on two accounts.</summary>
	SameParty = 1,

	/// <summary>Connected from the same IP address; the same household or the same person.</summary>
	SameNetwork = 2,

	/// <summary>One plays on the other's copy of the game through Steam Family Sharing.</summary>
	FamilyShare = 3
}

/// <summary>One account linked to another, with every reason and when it was last seen together.</summary>
internal sealed record AltLink( string SteamId, IReadOnlyList<AltEvidence> Evidence, DateTimeOffset LastSeenUtc )
{
	internal AltEvidence Strongest => Evidence.Max();
}

/// <summary>One IP address an account connected from, and when.</summary>
internal sealed record AltAddress( string Address, DateTimeOffset FirstSeenUtc, DateTimeOffset LastSeenUtc );

/// <summary>What Superuser remembers about one account.</summary>
internal sealed class AltIdentity
{
	internal const int MaximumAddresses = 5;
	internal const int MaximumNames = 4;
	internal const int MaximumMarks = 4;

	internal string SteamId { get; init; } = "";
	internal DateTimeOffset FirstSeenUtc { get; set; }
	internal DateTimeOffset LastSeenUtc { get; set; }
	internal List<string> Names { get; init; } = new();
	/// <summary>IP addresses, most recent first, with when each was first and last seen.</summary>
	internal List<AltAddress> Addresses { get; init; } = new();
	internal IEnumerable<string> Networks => Addresses.Select( address => address.Address );
	internal List<string> Owners { get; init; } = new();
	internal List<string> Parties { get; init; } = new();
}

/// <summary>
/// Remembers who connected with which family-share owner, network and party, and links accounts that share them.
/// Records are bounded per account and in total, and accounts not seen for a while are forgotten.
/// </summary>
internal sealed class AltLedger
{
	// Sized so the whole ledger fits one stored document (512 KB).
	internal const int MaximumIdentities = 500;
	internal static readonly TimeSpan Retention = TimeSpan.FromDays( 90 );

	private readonly Dictionary<string, AltIdentity> _identities = new( StringComparer.Ordinal );
	// Who shares each network, party and family-share copy; rebuilt only after records change, so finding an
	// account's alts is a few lookups rather than a pass over every account.
	private readonly Dictionary<string, List<string>> _byNetwork = new( StringComparer.Ordinal );
	private readonly Dictionary<string, List<string>> _byParty = new( StringComparer.Ordinal );
	private readonly Dictionary<string, List<string>> _byCopy = new( StringComparer.Ordinal );
	private bool _indexed;

	internal IReadOnlyCollection<AltIdentity> Identities => _identities.Values;

	internal AltIdentity Find( string steamId ) => _identities.TryGetValue( steamId, out var identity ) ? identity : null;

	/// <summary>Records a connection. Owner is the family-share owner (the same ID when not shared); address and party may be empty.</summary>
	internal void Record( string steamId, string name, string owner, string address, string party, DateTimeOffset now )
	{
		if ( !_identities.TryGetValue( steamId, out var identity ) )
			_identities[ steamId ] = identity = new AltIdentity { SteamId = steamId, FirstSeenUtc = now };
		identity.LastSeenUtc = now;
		Remember( identity.Names, name, AltIdentity.MaximumNames );
		if ( owner != steamId ) Remember( identity.Owners, owner, AltIdentity.MaximumMarks );
		RememberAddress( identity.Addresses, address, now );
		Remember( identity.Parties, party, AltIdentity.MaximumMarks );
		Prune( now );
		_indexed = false;
	}

	internal void Load( IEnumerable<AltIdentity> identities )
	{
		_identities.Clear();
		foreach ( var identity in identities.Take( MaximumIdentities ) ) _identities[ identity.SteamId ] = identity;
		_indexed = false;
	}

	/// <summary>Every account tied to this one, strongest evidence first.</summary>
	internal IReadOnlyList<AltLink> AltsOf( string steamId )
	{
		if ( Find( steamId ) is not { } identity ) return Array.Empty<AltLink>();
		Index();
		var links = new Dictionary<string, (HashSet<AltEvidence> Evidence, DateTimeOffset Seen)>( StringComparer.Ordinal );
		void Link( IEnumerable<string> others, AltEvidence evidence )
		{
			foreach ( var other in others )
			{
				if ( other == steamId ) continue;
				var seen = Find( other )?.LastSeenUtc ?? identity.LastSeenUtc;
				if ( !links.TryGetValue( other, out var link ) ) links[ other ] = link = (new HashSet<AltEvidence>(), seen);
				link.Evidence.Add( evidence );
			}
		}
		// Family sharing goes both ways: the copy's owner, and anyone else playing on the same copy.
		foreach ( var copy in identity.Owners.Append( steamId ) )
			if ( _byCopy.TryGetValue( copy, out var sharers ) ) Link( sharers.Append( copy ), AltEvidence.FamilyShare );
		foreach ( var network in identity.Networks )
			if ( _byNetwork.TryGetValue( network, out var sharing ) ) Link( sharing, AltEvidence.SameNetwork );
		foreach ( var party in identity.Parties )
			if ( _byParty.TryGetValue( party, out var members ) ) Link( members, AltEvidence.SameParty );
		return links.Select( pair => new AltLink( pair.Key, pair.Value.Evidence.OrderByDescending( evidence => evidence ).ToArray(), pair.Value.Seen ) )
			.OrderByDescending( link => link.Strongest ).ThenByDescending( link => link.LastSeenUtc ).ToArray();
	}

	/// <summary>
	/// Accounts worth a look, riskiest first: anyone linked to a banned account, then by the strength and number of
	/// their links. Accounts with no links aren't suspects.
	/// </summary>
	internal IReadOnlyList<(string SteamId, int Score, IReadOnlyList<AltLink> Alts)> Suspects( Func<string, bool> isBanned )
	{
		return _identities.Keys.Select( steamId =>
			{
				var alts = AltsOf( steamId );
				var score = alts.Sum( link => (int)link.Strongest * 10 + (isBanned( link.SteamId ) ? 100 : 0) );
				return (steamId, score, alts);
			} )
			.Where( suspect => suspect.alts.Count > 0 )
			.OrderByDescending( suspect => suspect.score ).ThenBy( suspect => suspect.steamId, StringComparer.Ordinal )
			.Select( suspect => (suspect.steamId, suspect.score, (IReadOnlyList<AltLink>)suspect.alts) ).ToArray();
	}

	private void Index()
	{
		if ( _indexed ) return;
		_byNetwork.Clear();
		_byParty.Clear();
		_byCopy.Clear();
		static void Add( Dictionary<string, List<string>> index, string key, string steamId )
		{
			if ( !index.TryGetValue( key, out var list ) ) index[ key ] = list = new List<string>();
			list.Add( steamId );
		}
		foreach ( var identity in _identities.Values )
		{
			foreach ( var network in identity.Networks ) Add( _byNetwork, network, identity.SteamId );
			foreach ( var party in identity.Parties ) Add( _byParty, party, identity.SteamId );
			foreach ( var owner in identity.Owners ) Add( _byCopy, owner, identity.SteamId );
		}
		_indexed = true;
	}

	private void Prune( DateTimeOffset now )
	{
		foreach ( var stale in _identities.Values.Where( identity => now - identity.LastSeenUtc > Retention ).Select( identity => identity.SteamId ).ToArray() )
			_identities.Remove( stale );
		if ( _identities.Count <= MaximumIdentities ) return;
		foreach ( var oldest in _identities.Values.OrderBy( identity => identity.LastSeenUtc ).Take( _identities.Count - MaximumIdentities ).Select( identity => identity.SteamId ).ToArray() )
			_identities.Remove( oldest );
	}

	// Most recent first: a known address keeps when it was first seen and moves to the front.
	private static void RememberAddress( List<AltAddress> addresses, string address, DateTimeOffset now )
	{
		if ( string.IsNullOrWhiteSpace( address ) ) return;
		var known = addresses.FindIndex( entry => entry.Address == address );
		var first = known >= 0 ? addresses[ known ].FirstSeenUtc : now;
		if ( known >= 0 ) addresses.RemoveAt( known );
		addresses.Insert( 0, new AltAddress( address, first, now ) );
		if ( addresses.Count > AltIdentity.MaximumAddresses ) addresses.RemoveRange( AltIdentity.MaximumAddresses, addresses.Count - AltIdentity.MaximumAddresses );
	}

	// Most recent first, without repeats, keeping the newest few.
	private static void Remember( List<string> values, string value, int maximum )
	{
		if ( string.IsNullOrWhiteSpace( value ) ) return;
		values.Remove( value );
		values.Insert( 0, value );
		if ( values.Count > maximum ) values.RemoveRange( maximum, values.Count - maximum );
	}
}
