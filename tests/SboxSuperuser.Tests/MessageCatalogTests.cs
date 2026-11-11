using SboxSuperuser.Core.Messages;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class MessageCatalogTests
{
	[Fact]
	public void BanReadsAsOneSentenceWithWordsForTheDuration()
	{
		var catalog = new MessageCatalog();
		Assert.Equal( "chomnr banned Smithers for 10 days for mic spam.",
			catalog.Render( "ban", "chomnr", "Smithers", "mic spam.", MessageCatalog.Phrase( TimeSpan.FromDays( 10 ) ) ) );
		Assert.Equal( "chomnr banned Smithers permanently for cheating.",
			catalog.Render( "ban", "chomnr", "Smithers", "cheating", MessageCatalog.Phrase( null ) ) );
		Assert.Equal( "for 1 day 2 hours 1 minute", MessageCatalog.Phrase( new TimeSpan( 1, 2, 1, 0 ) ) );
	}

	[Fact]
	public void OverridesReplaceTheDefaultAndOnlyTheirOwnPlaceholdersAreAllowed()
	{
		var catalog = new MessageCatalog();
		catalog.Load( new Dictionary<string, string> { [ "kick" ] = "{target} was shown the door by {actor} ({reason})", [ "tag" ] = "ADMIN" } );
		Assert.Equal( "Smithers was shown the door by chomnr (afk)", catalog.Render( "kick", "chomnr", "Smithers", "afk" ) );
		Assert.Equal( "ADMIN", catalog.Tag );
		Assert.Null( MessageCatalog.Problem( "ban", "{actor} removed {target} {duration}: {reason}" ) );
		Assert.Contains( "{duration}", MessageCatalog.Problem( "kick", "{actor} kicked {target} {duration}" ) );
		Assert.NotNull( MessageCatalog.Problem( "tag", "THIS TAG IS FAR TOO LONG TO FIT" ) );
		Assert.NotNull( MessageCatalog.Problem( "nothing", "x" ) );
		Assert.Throws<FormatException>( () => catalog.Load( new Dictionary<string, string> { [ "kick" ] = "line\nbreak" } ) );
		Assert.Equal( "ADMIN", catalog.Tag );
	}

	[Fact]
	public void EveryDefaultIsValidAndUnknownKeysRenderNull()
	{
		Assert.All( MessageCatalog.Defaults, entry => Assert.Null( MessageCatalog.Problem( entry.Key, entry.Value.Default ) ) );
		Assert.Null( new MessageCatalog().Render( "voicemute" ) );
	}
}
