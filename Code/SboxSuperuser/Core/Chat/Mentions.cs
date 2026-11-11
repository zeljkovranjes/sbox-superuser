using SboxSuperuser.Core.Commands;

namespace SboxSuperuser.Core.Chat;

/// <summary>One @name in a message: where it is and whose name it matched.</summary>
internal readonly record struct Mention( int Start, int Length, string Name );

/// <summary>
/// Discord-style @mentions: finds every @name in a message that matches a connected player, and completes the name
/// being typed. Names may contain spaces, so the longest matching name wins at each @.
/// </summary>
internal static class Mentions
{
	internal const char Marker = '@';

	/// <summary>Every mention in the message, in order; one message may mention any number of players.</summary>
	internal static IReadOnlyList<Mention> Find( string message, IReadOnlyList<string> names )
	{
		var found = new List<Mention>();
		if ( string.IsNullOrEmpty( message ) || names.Count == 0 ) return found;
		var index = message.IndexOf( Marker );
		while ( index >= 0 && index < message.Length - 1 )
		{
			// An @ inside a word, like an email address, is not a mention.
			var standalone = index == 0 || char.IsWhiteSpace( message[ index - 1 ] );
			var name = standalone ? Longest( message, index + 1, names ) : null;
			if ( name is not null ) found.Add( new Mention( index, name.Length + 1, name ) );
			index = message.IndexOf( Marker, index + 1 + (name?.Length ?? 0) );
		}
		return found;
	}

	/// <summary>Whether the message mentions this name, ignoring case.</summary>
	internal static bool MentionsName( string message, string name, IReadOnlyList<string> names ) =>
		Find( message, names ).Any( mention => string.Equals( mention.Name, name, StringComparison.OrdinalIgnoreCase ) );

	/// <summary>Suggests names for an @ being typed at the end of the line; empty when the line doesn't end in one.</summary>
	internal static Completion Complete( string line, IReadOnlyList<string> names )
	{
		if ( string.IsNullOrEmpty( line ) ) return Completion.None;
		var at = line.LastIndexOf( Marker );
		if ( at < 0 || at > 0 && !char.IsWhiteSpace( line[ at - 1 ] ) ) return Completion.None;
		var typing = line.Substring( at + 1 );
		// A finished mention followed by a space ends the suggestion, unless the name itself has spaces still being typed.
		var suggestions = names.Where( name => name.StartsWith( typing, StringComparison.OrdinalIgnoreCase )
				&& !string.Equals( name, typing, StringComparison.OrdinalIgnoreCase ) )
			.OrderBy( name => name, StringComparer.OrdinalIgnoreCase ).Take( CommandCompletion.MaximumSuggestions ).ToArray();
		return suggestions.Length == 0 ? Completion.None
			: new Completion( "", null, Array.Empty<CompletionArgument>(), suggestions, line.Substring( 0, at + 1 ) );
	}

	private static string Longest( string message, int start, IReadOnlyList<string> names )
	{
		string best = null;
		foreach ( var name in names )
		{
			if ( name.Length == 0 || start + name.Length > message.Length || best is not null && name.Length <= best.Length ) continue;
			if ( string.Compare( message, start, name, 0, name.Length, StringComparison.OrdinalIgnoreCase ) != 0 ) continue;
			// The name must end at a word boundary, so @Al doesn't match inside @Alice.
			var end = start + name.Length;
			if ( end < message.Length && char.IsLetterOrDigit( message[ end ] ) ) continue;
			best = name;
		}
		return best;
	}
}
