using System.Text.RegularExpressions;

namespace SboxSuperuser.Core.Content;

/// <summary>One banned map, addon or entity; the pattern may use * for any run of characters.</summary>
internal sealed record ContentBan( string Kind, string Pattern, string Reason );

/// <summary>
/// The server's banned content. Kinds are map (map idents or paths), addon (package idents such as author.pack)
/// and entity (prefab paths, model paths or component type names). Matching ignores case, and * matches anything,
/// so "author.*" bans every package from one author.
/// </summary>
internal sealed class ContentBanList
{
	internal const string Map = "map", Addon = "addon", Entity = "entity";
	internal static readonly IReadOnlyList<string> Kinds = new[] { Map, Addon, Entity };
	internal const int MaximumPatternLength = 128;
	internal const int MaximumBans = 512;

	private readonly List<(ContentBan Ban, Regex Match)> _bans = new();

	internal IReadOnlyList<ContentBan> Bans => _bans.Select( entry => entry.Ban ).ToArray();

	/// <summary>Why the ban can't be added, or null when it can.</summary>
	internal static string Problem( string kind, string pattern )
	{
		if ( !Kinds.Contains( kind ) ) return $"Content is one of {string.Join( ", ", Kinds )}.";
		if ( string.IsNullOrWhiteSpace( pattern ) || pattern.Length > MaximumPatternLength || pattern.Any( character => char.IsControl( character ) || char.IsWhiteSpace( character ) ) )
			return $"Patterns are one word of at most {MaximumPatternLength} characters.";
		return pattern.Trim( '*' ).Length == 0 ? "A pattern must name something besides *." : null;
	}

	/// <summary>Replaces every ban at once, as loaded from storage; any invalid entry refuses the whole list.</summary>
	internal void Load( IEnumerable<ContentBan> bans )
	{
		var loaded = bans.ToArray();
		if ( loaded.Length > MaximumBans ) throw new FormatException( "Too many content bans." );
		foreach ( var ban in loaded ) if ( Problem( ban.Kind, ban.Pattern ) is { } problem ) throw new FormatException( problem );
		_bans.Clear();
		foreach ( var ban in loaded ) _bans.Add( (ban, Compile( ban.Pattern )) );
	}

	/// <summary>The ban that covers this item, or null when it is allowed.</summary>
	internal ContentBan Find( string kind, string candidate )
	{
		if ( string.IsNullOrEmpty( candidate ) ) return null;
		foreach ( var (ban, match) in _bans )
			if ( ban.Kind == kind && match.IsMatch( candidate ) ) return ban;
		return null;
	}

	private static Regex Compile( string pattern ) => new( "^" + string.Join( ".*", pattern.Split( '*' ).Select( Regex.Escape ) ) + "$",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );
}
