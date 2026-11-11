using SboxSuperuser.Core.Chat;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class MentionsTests
{
	private static readonly string[] Names = { "Marge", "Smithers", "Al", "Alice Cooper" };

	[Fact]
	public void FindsSeveralMentionsAndPrefersTheLongestName()
	{
		var found = Mentions.Find( "hey @marge and @Alice Cooper, also @Al!", Names );
		Assert.Equal( new[] { "Marge", "Alice Cooper", "Al" }, found.Select( mention => mention.Name ) );
		Assert.Equal( 4, found[ 0 ].Start );
		Assert.Equal( 6, found[ 0 ].Length );
	}

	[Fact]
	public void IgnoresEmailsUnknownNamesAndPartialWords()
	{
		Assert.Empty( Mentions.Find( "mail me at bob@marge.com or @nobody or @Alfred", Names ) );
		Assert.True( Mentions.MentionsName( "thanks @SMITHERS", "Smithers", Names ) );
		Assert.False( Mentions.MentionsName( "thanks @Marge", "Smithers", Names ) );
	}

	[Fact]
	public void CompletesTheNameBeingTyped()
	{
		var completion = Mentions.Complete( "nice one @sm", Names );
		Assert.Equal( new[] { "Smithers" }, completion.Suggestions );
		Assert.Equal( "nice one @Smithers ", completion.Accept( "Smithers" ) );
		Assert.Equal( new[] { "Al", "Alice Cooper" }, Mentions.Complete( "@a", Names ).Suggestions );
		Assert.True( Mentions.Complete( "no mention here", Names ).IsEmpty );
		Assert.True( Mentions.Complete( "@Smithers", Names ).IsEmpty );
	}
}
