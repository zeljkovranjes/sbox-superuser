using SboxSuperuser.Core.Menu;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class MenuPreferencesTests
{
	[Fact]
	public void RecentReasonsAreNewestFirstDistinctAndBounded()
	{
		var preferences = new MenuPreferences();
		for ( var i = 0; i < 10; i++ ) preferences.Remember( $"Reason {i}", "" );
		preferences.Remember( "Reason 5", "" );

		Assert.Equal( MenuPreferences.MaximumReasons, preferences.RecentReasons.Count );
		Assert.Equal( "Reason 5", preferences.RecentReasons[ 0 ] );
		Assert.Single( preferences.RecentReasons, reason => reason == "Reason 5" );
		Assert.DoesNotContain( "Reason 0", preferences.RecentReasons );
	}

	[Fact]
	public void LastDurationIsKeptAndBlankDurationsAreIgnored()
	{
		var preferences = new MenuPreferences();
		preferences.Remember( "Spam", "2h" );
		preferences.Remember( "Spam", "" );
		Assert.Equal( "2h", preferences.LastDuration );
	}

	[Fact]
	public void RepeatingTheSameChoiceDoesNotRaiseTheRevision()
	{
		var preferences = new MenuPreferences();
		preferences.Remember( "Spam", "2h" );
		var revision = preferences.Revision;
		preferences.Remember( "Spam", "2h" );
		Assert.Equal( revision, preferences.Revision );
	}

	[Fact]
	public void PreferencesSurviveARoundTrip()
	{
		var preferences = new MenuPreferences();
		preferences.Remember( "Cheating", "permanent" );
		preferences.Remember( "Spam", "30m" );

		var restored = MenuPreferences.Parse( preferences.Write() );
		Assert.Equal( new[] { "Spam", "Cheating" }, restored.RecentReasons );
		Assert.Equal( "30m", restored.LastDuration );
	}

	[Theory]
	[InlineData( "{\"schemaVersion\":2,\"recentReasons\":[],\"lastDuration\":\"\"}" )]
	[InlineData( "{\"schemaVersion\":1,\"recentReasons\":[\"a\",\"a\"],\"lastDuration\":\"\"}" )]
	[InlineData( "{\"schemaVersion\":1,\"recentReasons\":[\"bad\\nline\"],\"lastDuration\":\"\"}" )]
	[InlineData( "{\"schemaVersion\":1,\"recentReasons\":[1],\"lastDuration\":\"\"}" )]
	[InlineData( "{\"schemaVersion\":1,\"recentReasons\":[]}" )]
	public void InvalidPreferencesAreRejected( string json )
	{
		Assert.ThrowsAny<Exception>( () => MenuPreferences.Parse( json ) );
	}
}
