using SboxSuperuser.Core.Chat;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class SentHistoryTests
{
	[Fact]
	public void UpWalksBackAndDownReturnsToTheDraft()
	{
		var history = new SentHistory();
		history.Add( "first" );
		history.Add( "second" );

		Assert.Equal( "second", history.Older( "half typed" ) );
		Assert.Equal( "first", history.Older( "second" ) );
		Assert.Null( history.Older( "first" ) );
		Assert.Equal( "second", history.Newer() );
		Assert.Equal( "half typed", history.Newer() );
		Assert.Null( history.Newer() );
	}

	[Fact]
	public void RepeatsAndBlanksAreNotStoredAndTheOldestDropOff()
	{
		var history = new SentHistory();
		history.Add( "gg" );
		history.Add( "gg" );
		history.Add( "  " );
		for ( var index = 0; index < SentHistory.Capacity; index++ ) history.Add( $"line {index}" );

		var seen = new List<string>();
		for ( var text = history.Older( "" ); text is not null; text = history.Older( text ) ) seen.Add( text );

		Assert.Equal( SentHistory.Capacity, seen.Count );
		Assert.DoesNotContain( "gg", seen );
		Assert.Equal( "line 19", seen[ 0 ] );
	}
}
