using SboxSuperuser.Core.Chat;
using SboxSuperuser.Core.Menu;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class MenuHelpersTests
{
	private static readonly DateTimeOffset Now = new( 2026, 10, 18, 12, 0, 0, TimeSpan.Zero );

	[Theory]
	[InlineData( -5, "expiring" )]
	[InlineData( 30, "under 1m" )]
	[InlineData( 45 * 60, "45m" )]
	[InlineData( 2 * 3600 + 14 * 60 + 59, "2h 14m" )]
	[InlineData( 3 * 86400 + 5 * 3600, "3d 5h" )]
	public void TimeRemainingReadsNaturally( int seconds, string expected )
	{
		Assert.Equal( expected, TimeRemaining.Format( TimeSpan.FromSeconds( seconds ) ) );
	}

	[Fact]
	public void NoticesFadeOutAndKeepOnlyTheNewest()
	{
		var notices = new Notices<string>();
		for ( var i = 0; i < 6; i++ ) notices.Add( $"n{i}", Now );
		var shown = notices.Visible( Now );
		Assert.Equal( Notices<string>.MaximumShown, shown.Count );
		Assert.Equal( "n5", shown[ ^1 ].Value );
		Assert.Equal( 1f, shown[ 0 ].Opacity );

		var fading = notices.Visible( Now + TimeSpan.FromSeconds( 4.5 ) );
		Assert.InRange( fading[ 0 ].Opacity, 0.4f, 0.6f );
		Assert.Empty( notices.Visible( Now + Notices<string>.Lifetime ) );
	}

	[Fact]
	public void RecentChatIsBoundedNewestFirstAndFiltersByPlayer()
	{
		var chat = new RecentChat();
		for ( var i = 0; i < RecentChat.Capacity + 10; i++ ) chat.Record( i % 2 == 0 ? "1" : "2", "Name", $"line {i}", Now.AddSeconds( i ) );
		Assert.Equal( RecentChat.Capacity, chat.Count );
		Assert.Equal( $"line {RecentChat.Capacity + 9}", chat.Read()[ 0 ].Message );
		Assert.All( chat.Read( "1" ), line => Assert.Equal( "1", line.SteamId ) );
	}

	[Fact]
	public void RecentChatCannotForgeLinesOrStoreBlankMessages()
	{
		var chat = new RecentChat();
		chat.Record( "1", "Bad\nName", "hello\r\n[Superuser] fake", Now );
		chat.Record( "1", "Name", "   ", Now );
		var line = Assert.Single( chat.Read() );
		Assert.DoesNotContain( '\n', line.Message );
		Assert.DoesNotContain( '\n', line.Name );
		chat.Clear();
		Assert.Equal( 0, chat.Count );
	}
}
