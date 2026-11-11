using System.Text.RegularExpressions;

namespace SboxSuperuser.Core.Messages;

/// <summary>
/// Every line Superuser shows players, by key, with the server's own wording where it has changed one. A template
/// is one printable line; {placeholders} name the values it is filled with, and each key allows only its own.
/// </summary>
internal sealed class MessageCatalog
{
	internal const int MaximumLength = 256;
	internal const int MaximumTagLength = 24;
	internal const string TagKey = "tag";

	private const string Actor = "actor", Target = "target", Reason = "reason", Duration = "duration", Map = "map", Text = "message";
	private static readonly Regex Placeholder = new( @"\{([a-z]+)\}", RegexOptions.CultureInvariant );

	/// <summary>The built-in wording and the placeholders each key accepts.</summary>
	internal static IReadOnlyDictionary<string, (string Default, string[] Placeholders)> Defaults { get; } = Build();

	private readonly Dictionary<string, string> _overrides = new( StringComparer.Ordinal );

	/// <summary>Only the keys the server has changed.</summary>
	internal IReadOnlyDictionary<string, string> Overrides => _overrides;

	internal string Template( string key ) => _overrides.TryGetValue( key, out var custom ) ? custom : Defaults[ key ].Default;

	/// <summary>The tag shown in front of public announcements, such as SUPERUSER.</summary>
	internal string Tag => Template( TagKey );

	/// <summary>Fills a key's template; unknown keys return null so callers can fall back.</summary>
	/// <param name="duration">Already phrased by <see cref="Phrase"/>, such as "for 10 days".</param>
	internal string Render( string key, string actor = null, string target = null, string reason = null, string duration = null, string map = null, string message = null )
	{
		if ( !Defaults.ContainsKey( key ) ) return null;
		var values = new Dictionary<string, string>
		{
			[ Actor ] = actor ?? "Console", [ Target ] = target ?? "", [ Reason ] = reason?.Trim().TrimEnd( '.', '!', '?' ) ?? "",
			[ Duration ] = duration ?? "", [ Map ] = map ?? "", [ Text ] = message ?? ""
		};
		return Placeholder.Replace( Template( key ), match => values.TryGetValue( match.Groups[ 1 ].Value, out var value ) ? value : match.Value );
	}

	/// <summary>Why a template can't be used for this key, or null when it can.</summary>
	internal static string Problem( string key, string template )
	{
		if ( !Defaults.TryGetValue( key, out var entry ) ) return $"There is no message called {key}.";
		var maximum = key == TagKey ? MaximumTagLength : MaximumLength;
		if ( string.IsNullOrWhiteSpace( template ) || template.Length > maximum || template.Any( char.IsControl ) )
			return $"Messages are one printable line of at most {maximum} characters.";
		foreach ( Match match in Placeholder.Matches( template ) )
			if ( !entry.Placeholders.Contains( match.Groups[ 1 ].Value ) )
				return $"{key} can use {(entry.Placeholders.Length == 0 ? "no placeholders" : string.Join( ", ", entry.Placeholders.Select( name => "{" + name + "}" ) ))}, not {match.Value}.";
		return null;
	}

	/// <summary>Replaces every override at once, as loaded from storage; invalid entries are refused whole.</summary>
	internal void Load( IReadOnlyDictionary<string, string> overrides )
	{
		foreach ( var (key, template) in overrides )
			if ( Problem( key, template ) is { } problem ) throw new FormatException( problem );
		_overrides.Clear();
		foreach ( var (key, template) in overrides ) _overrides[ key ] = template;
	}

	/// <summary>"for 10 days", "for 2 hours 30 minutes", or "permanently" when there is no end.</summary>
	internal static string Phrase( TimeSpan? length )
	{
		if ( length is not { } time ) return "permanently";
		var parts = new List<string>();
		void Add( int amount, string unit ) { if ( amount > 0 ) parts.Add( $"{amount} {unit}{(amount == 1 ? "" : "s")}" ); }
		Add( time.Days, "day" );
		Add( time.Hours, "hour" );
		Add( time.Minutes, "minute" );
		if ( parts.Count == 0 ) Add( Math.Max( 1, time.Seconds ), "second" );
		return "for " + string.Join( " ", parts );
	}

	private static IReadOnlyDictionary<string, (string, string[])> Build()
	{
		var entries = new Dictionary<string, (string, string[])>( StringComparer.Ordinal )
		{
			[ TagKey ] = ("SUPERUSER", Array.Empty<string>()),
			[ "psay" ] = ("[PM] {actor}: {message}", new[] { Actor, Text }),
			[ "asay" ] = ("[STAFF] {actor}: {message}", new[] { Actor, Text }),
			[ "map" ] = ("{actor} is changing the map to {map}.", new[] { Actor, Map }),
			[ "join" ] = ("{target} joined the game.", new[] { Target }),
			[ "leave" ] = ("{target} left the game.", new[] { Target })
		};
		void Restriction( string key, string verb ) => entries[ key ] = ($"{{actor}} {verb} {{target}} {{duration}} for {{reason}}.", new[] { Actor, Target, Duration, Reason });
		void Reasoned( string key, string verb ) => entries[ key ] = ($"{{actor}} {verb} {{target}} for {{reason}}.", new[] { Actor, Target, Reason });
		void Plain( string key, string verb ) => entries[ key ] = ($"{{actor}} {verb} {{target}}.", new[] { Actor, Target });
		Restriction( "ban", "banned" );
		Restriction( "mute", "muted" );
		Restriction( "gag", "gagged" );
		Restriction( "gimp", "gimped" );
		Reasoned( "kick", "kicked" );
		Reasoned( "warn", "warned" );
		Reasoned( "unban", "unbanned" );
		Reasoned( "unmute", "unmuted" );
		Reasoned( "ungag", "ungagged" );
		Reasoned( "ungimp", "ungimped" );
		foreach ( var (key, verb) in new[]
		{
			("slay", "slayed"), ("slap", "slapped"), ("whip", "whipped"), ("maul", "mauled"), ("freeze", "froze"), ("unfreeze", "unfroze"),
			("blind", "blinded"), ("unblind", "unblinded"), ("ignite", "set fire to"), ("unignite", "put out"), ("jail", "jailed"),
			("unjail", "released"), ("strip", "stripped"), ("ragdoll", "ragdolled"), ("unragdoll", "stood up"), ("god", "gave god mode to"),
			("ungod", "took god mode from")
		} ) Plain( key, verb );
		return entries;
	}
}
