namespace SboxSuperuser.Core.Commands;

/// <summary>What an argument expects, as far as completion needs to know.</summary>
internal enum CompletionKind
{
	Text,
	Number,
	Player,
	Duration,
	Boolean,
	Choice
}

internal sealed record CompletionArgument( string Name, CompletionKind Kind, bool Optional, bool Remainder, IReadOnlyList<string> Choices,
	string Description = "" );

/// <summary>A command as typed in chat: words separated by spaces, such as "mute" or "messages set".</summary>
internal sealed record CompletionCommand( string Words, IReadOnlyList<CompletionArgument> Arguments, string Description = "" );

/// <summary>
/// Where a chat line is in a command: the words typed so far, which argument comes next and what fits there. Empty
/// when the line isn't a command or is already complete, so the hint disappears once nothing more is needed.
/// </summary>
/// <param name="Notes">A short line for each suggestion, such as what a command does; empty when there is none.</param>
internal sealed record Completion( string Typed, CompletionArgument Current, IReadOnlyList<CompletionArgument> Remaining,
	IReadOnlyList<string> Suggestions, string Prefix, IReadOnlyList<string> Notes = null )
{
	internal string NoteFor( int index ) => Notes is not null && index < Notes.Count ? Notes[ index ] : "";

	internal static Completion None { get; } = new( "", null, Array.Empty<CompletionArgument>(), Array.Empty<string>(), "" );

	internal bool IsEmpty => Current is null && Suggestions.Count == 0;

	/// <summary>The line with the word being typed replaced by the suggestion, ready for the next word.</summary>
	internal string Accept( string suggestion ) => Prefix + suggestion + " ";
}

internal static class CommandCompletion
{
	internal const int MaximumSuggestions = 6;
	internal static readonly IReadOnlyList<string> DurationPresets = new[] { "30m", "1h", "1d", "7d", "permanent" };
	private static readonly IReadOnlyList<string> Booleans = new[] { "true", "false" };

	/// <param name="root">The chat command prefix without the slash, such as admin.</param>
	internal static Completion Complete( string line, string root, IReadOnlyList<CompletionCommand> commands, IReadOnlyList<string> players )
	{
		if ( line is not { Length: > 0 } || line[ 0 ] != '/' ) return Completion.None;
		// Both "/admin mute" and the shorter "/mute" work; the root is added when the line is sent.
		var start = "/" + root + " ";
		var rooted = line.StartsWith( start, StringComparison.OrdinalIgnoreCase );
		var lead = rooted ? start : "/";
		var body = line.Substring( lead.Length );
		var words = body.Split( ' ' );
		// The last word is still being typed; everything before it is settled.
		var finished = words.Take( words.Length - 1 ).Where( word => word.Length > 0 ).ToArray();
		var typing = words[ ^1 ];
		var settledPrefix = line.Substring( 0, line.Length - typing.Length );

		var command = commands.Where( candidate => Matches( candidate.Words, finished ) ).OrderByDescending( candidate => candidate.Words.Length ).FirstOrDefault();
		if ( command is null )
		{
			var spoken = string.Join( ' ', finished.Append( typing ) );
			var names = commands.Select( candidate => candidate.Words ).Where( words => words.StartsWith( spoken, StringComparison.OrdinalIgnoreCase ) )
				.OrderBy( words => words, StringComparer.Ordinal ).Take( MaximumSuggestions )
				// Suggest only the next word of a multi-word command, so "mess" offers "messages" first.
				.Select( words => words.Split( ' ' )[ finished.Length ] ).Distinct().ToArray();
			// While the first word is typed, the root itself is a suggestion too.
			if ( !rooted && finished.Length == 0 && root.StartsWith( typing, StringComparison.OrdinalIgnoreCase ) && !names.Contains( root ) )
				names = names.Prepend( root ).Take( MaximumSuggestions ).ToArray();
			var spokenPrefix = finished.Length == 0 ? "" : string.Join( ' ', finished ) + " ";
			var notes = names.Select( name => commands.FirstOrDefault( candidate =>
				candidate.Words.StartsWith( spokenPrefix + name, StringComparison.OrdinalIgnoreCase ) )?.Description ?? "" ).ToArray();
			return new Completion( string.Join( ' ', finished ), null, Array.Empty<CompletionArgument>(), names, settledPrefix, notes );
		}

		// Past the last argument, or into the second word of a closing free-text argument, nothing more is needed.
		var argumentIndex = finished.Length - command.Words.Split( ' ' ).Length;
		var arguments = command.Arguments;
		if ( argumentIndex >= arguments.Count ) return Completion.None;
		var current = arguments[ argumentIndex ];
		var remaining = arguments.Skip( argumentIndex + 1 ).ToArray();
		return new Completion( lead + string.Join( ' ', finished ), current, remaining, Suggest( current, typing, players ), settledPrefix );
	}

	/// <summary>
	/// "/mute Marge 1d spam" becomes "/admin mute Marge 1d spam" when mute is one of the player's commands; any
	/// other line is returned unchanged, so a game's own slash commands keep working.
	/// </summary>
	internal static string Expand( string line, string root, IReadOnlyList<CompletionCommand> commands )
	{
		if ( line is not { Length: > 1 } || line[ 0 ] != '/' || line.StartsWith( "/" + root + " ", StringComparison.OrdinalIgnoreCase )
			|| string.Equals( line, "/" + root, StringComparison.OrdinalIgnoreCase ) ) return line;
		var words = line.Substring( 1 ).Split( ' ', StringSplitOptions.RemoveEmptyEntries );
		return commands.Any( command => Matches( command.Words, words ) ) ? "/" + root + " " + line.Substring( 1 ) : line;
	}

	private static bool Matches( string commandWords, IReadOnlyList<string> finished )
	{
		var parts = commandWords.Split( ' ' );
		if ( finished.Count < parts.Length ) return false;
		for ( var index = 0; index < parts.Length; index++ )
			if ( !string.Equals( parts[ index ], finished[ index ], StringComparison.OrdinalIgnoreCase ) ) return false;
		return true;
	}

	private static IReadOnlyList<string> Suggest( CompletionArgument argument, string typing, IReadOnlyList<string> players )
	{
		var options = argument.Kind switch
		{
			CompletionKind.Player => players,
			CompletionKind.Duration => DurationPresets,
			CompletionKind.Boolean => Booleans,
			CompletionKind.Choice => argument.Choices,
			_ => Array.Empty<string>()
		};
		// Names with spaces can't be typed as one word, so they are left out rather than suggested broken.
		return options.Where( option => !option.Contains( ' ' ) && option.StartsWith( typing, StringComparison.OrdinalIgnoreCase )
				&& !string.Equals( option, typing, StringComparison.OrdinalIgnoreCase ) )
			.Take( MaximumSuggestions ).ToArray();
	}
}
