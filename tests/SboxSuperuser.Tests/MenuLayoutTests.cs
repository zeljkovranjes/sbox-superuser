using SboxSuperuser.Core.Menu;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class MenuLayoutTests
{
	[Fact]
	public void PinnedWindowsStayVisibleWhileTheMenuIsClosed()
	{
		var layout = new MenuLayout();
		layout.SetOpen( "players", true );
		layout.SetOpen( "activity", true );
		layout.TogglePin( "activity" );

		Assert.True( layout.HasPinned );
		Assert.True( layout.IsVisible( "players", true ) );
		Assert.False( layout.IsVisible( "players", false ) );
		Assert.True( layout.IsVisible( "activity", false ) );
	}

	[Fact]
	public void ClosingAWindowUnpinsItAndPinningOpensIt()
	{
		var layout = new MenuLayout();
		layout.TogglePin( "activity" );
		Assert.True( layout.Window( "activity" ).Open );

		layout.SetOpen( "activity", false );
		Assert.False( layout.Window( "activity" ).Pinned );
		Assert.False( layout.HasPinned );
	}

	[Fact]
	public void LayoutSurvivesARoundTrip()
	{
		var layout = new MenuLayout();
		layout.TogglePin( "activity" );
		layout.SetBounds( "activity", 40, 60, 700, 300 );
		layout.SetOpen( "players", true );

		var restored = MenuLayout.Parse( layout.Write() );
		var activity = restored.Window( "activity" );
		Assert.True( activity.Pinned );
		Assert.True( activity.HasBounds );
		Assert.Equal( new[] { 40f, 60f, 700f, 300f }, new[] { activity.X, activity.Y, activity.Width, activity.Height } );
		Assert.True( restored.Window( "players" ).Open );
		Assert.False( restored.Window( "players" ).HasBounds );
	}

	[Fact]
	public void OnlyChangesRaiseTheRevision()
	{
		var layout = new MenuLayout();
		layout.SetBounds( "players", 1, 2, 3, 4 );
		var revision = layout.Revision;
		layout.SetBounds( "players", 1, 2, 3, 4 );
		layout.SetOpen( "players", false );
		Assert.Equal( revision, layout.Revision );
	}

	[Fact]
	public void WindowsMovedOffASmallerScreenComeBack()
	{
		var layout = new MenuLayout();
		layout.SetBounds( "players", 3000, 2000, 400, 500 );
		layout.FitTo( 1280, 720 );
		var players = layout.Window( "players" );
		Assert.InRange( players.X, 0, 1280 - 48 );
		Assert.InRange( players.Y, 0, 720 - 48 );
	}

	[Theory]
	[InlineData( "" )]
	[InlineData( "{}" )]
	[InlineData( "{\"schemaVersion\":2,\"windows\":{}}" )]
	[InlineData( "{\"schemaVersion\":1,\"windows\":{\"Bad Name\":{}}}" )]
	[InlineData( "{\"schemaVersion\":1,\"windows\":{\"players\":{\"open\":1}}}" )]
	[InlineData( "{\"schemaVersion\":1,\"windows\":{\"players\":{\"bounds\":[0,0,-5,10]}}}" )]
	public void InvalidLayoutsAreRejected( string json )
	{
		Assert.ThrowsAny<Exception>( () => MenuLayout.Parse( json ) );
	}

	[Fact]
	public void UnpinnedClosedWindowCannotLoadAsPinned()
	{
		var layout = MenuLayout.Parse( "{\"schemaVersion\":1,\"windows\":{\"activity\":{\"open\":false,\"pinned\":true}}}" );
		Assert.False( layout.HasPinned );
	}
}
