using SboxSuperuser.Core.Commands;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class CommandCompletionTests
{
	private static readonly CompletionArgument User = new( "user", CompletionKind.Player, false, false, Array.Empty<string>() );
	private static readonly CompletionArgument Duration = new( "duration", CompletionKind.Duration, false, false, Array.Empty<string>() );
	private static readonly CompletionArgument Reason = new( "reason", CompletionKind.Text, false, true, Array.Empty<string>() );
	private static readonly CompletionCommand[] Commands =
	{
		new( "mute", new[] { User, Duration, Reason }, "Stop a player's text chat" ),
		new( "moderation history", new[] { User } ),
		new( "messages set", Array.Empty<CompletionArgument>() ),
		new( "thetime", Array.Empty<CompletionArgument>() )
	};
	private static readonly string[] Players = { "Smithers", "Marge", "Two Words" };

	private static Completion Complete( string line ) => CommandCompletion.Complete( line, "admin", Commands, Players );

	[Fact]
	public void SuggestsTheRootThenCommandsThenTheNextWord()
	{
		Assert.Equal( new[] { "admin" }, Complete( "/adm" ).Suggestions );
		Assert.Equal( "/admin ", Complete( "/adm" ).Accept( "admin" ) );
		Assert.Equal( new[] { "messages", "moderation", "mute" }, Complete( "/admin m" ).Suggestions );
		Assert.Equal( new[] { "history" }, Complete( "/admin moderation h" ).Suggestions );
		Assert.True( Complete( "hello" ).IsEmpty );
	}

	[Fact]
	public void WalksThroughEachArgumentAndStopsWhenDone()
	{
		var user = Complete( "/admin mute " );
		Assert.Equal( "user", user.Current.Name );
		Assert.Equal( new[] { "duration", "reason" }, user.Remaining.Select( argument => argument.Name ) );
		Assert.Equal( new[] { "Smithers", "Marge" }, user.Suggestions );
		Assert.Equal( new[] { "Marge" }, Complete( "/admin mute ma" ).Suggestions );
		Assert.Equal( "/admin mute Marge ", Complete( "/admin mute ma" ).Accept( "Marge" ) );

		var duration = Complete( "/admin mute Marge " );
		Assert.Equal( "duration", duration.Current.Name );
		Assert.Contains( "permanent", duration.Suggestions );

		Assert.Equal( "reason", Complete( "/admin mute Marge 1d " ).Current.Name );
		Assert.True( Complete( "/admin mute Marge 1d spamming the " ).IsEmpty );
		Assert.True( Complete( "/admin thetime " ).IsEmpty );
	}

	[Fact]
	public void ShortCommandsWorkWithoutTheRootAndGainItWhenSent()
	{
		Assert.Equal( new[] { "mute" }, Complete( "/mu" ).Suggestions );
		Assert.Equal( "/mute ", Complete( "/mu" ).Accept( "mute" ) );
		var user = Complete( "/mute " );
		Assert.Equal( "user", user.Current.Name );
		Assert.Equal( "/mute", user.Typed );
		Assert.Equal( "duration", Complete( "/mute Marge " ).Current.Name );
		Assert.Equal( "/admin mute Marge 1d spam", CommandCompletion.Expand( "/mute Marge 1d spam", "admin", Commands ) );
		Assert.Equal( "/admin moderation history Marge", CommandCompletion.Expand( "/moderation history Marge", "admin", Commands ) );
		Assert.Equal( "/admin mute Marge", CommandCompletion.Expand( "/admin mute Marge", "admin", Commands ) );
		Assert.Equal( "/dance now", CommandCompletion.Expand( "/dance now", "admin", Commands ) );
		Assert.Equal( "hello", CommandCompletion.Expand( "hello", "admin", Commands ) );
	}

	[Fact]
	public void CommandSuggestionsCarryWhatTheyDo()
	{
		var completion = Complete( "/mu" );
		Assert.Equal( "mute", completion.Suggestions[ 0 ] );
		Assert.Equal( "Stop a player's text chat", completion.NoteFor( 0 ) );
		Assert.Equal( "", Complete( "/mute " ).NoteFor( 0 ) );
	}
}
