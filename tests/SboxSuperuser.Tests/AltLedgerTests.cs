using SboxSuperuser.Core.Alts;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class AltLedgerTests
{
	private static readonly DateTimeOffset Now = new( 2026, 11, 11, 0, 0, 0, TimeSpan.Zero );

	[Fact]
	public void LinksFamilySharingNetworksAndPartiesStrongestFirst()
	{
		var ledger = new AltLedger();
		ledger.Record( "main", "Bob", "main", "net1", "", Now );
		ledger.Record( "alt", "B0b", "main", "net2", "", Now );
		ledger.Record( "roommate", "Sam", "roommate", "net1", "partyA", Now );
		ledger.Record( "friend", "Al", "friend", "net9", "partyA", Now );

		var alts = ledger.AltsOf( "main" );
		Assert.Equal( new[] { "alt", "roommate" }, alts.Select( link => link.SteamId ) );
		Assert.Equal( AltEvidence.FamilyShare, alts[ 0 ].Strongest );
		Assert.Equal( AltEvidence.SameNetwork, alts[ 1 ].Strongest );
		Assert.Equal( new[] { "main" }, ledger.AltsOf( "alt" ).Select( link => link.SteamId ) );
		Assert.Equal( AltEvidence.SameParty, Assert.Single( ledger.AltsOf( "friend" ) ).Strongest );
	}

	[Fact]
	public void FamilyShareOwnersCountEvenIfTheyNeverJoined()
	{
		var ledger = new AltLedger();
		ledger.Record( "borrower", "X", "lender", "", "", Now );
		Assert.Equal( "lender", Assert.Single( ledger.AltsOf( "borrower" ) ).SteamId );
	}

	[Fact]
	public void SuspectsLinkedToBannedAccountsComeFirst()
	{
		var ledger = new AltLedger();
		ledger.Record( "evader", "E", "evader", "net1", "", Now );
		ledger.Record( "banned", "B", "banned", "net1", "", Now );
		ledger.Record( "a", "A", "a", "", "p", Now );
		ledger.Record( "b", "B2", "b", "", "p", Now );
		ledger.Record( "loner", "L", "loner", "net7", "", Now );
		var suspects = ledger.Suspects( steamId => steamId == "banned" );
		Assert.Equal( "evader", suspects[ 0 ].SteamId );
		Assert.DoesNotContain( suspects, suspect => suspect.SteamId == "loner" );
	}

	[Fact]
	public void RecordsStayBoundedAndOldAccountsAreForgotten()
	{
		var ledger = new AltLedger();
		for ( var name = 0; name < 20; name++ ) ledger.Record( "p", "name" + name, "p", "net" + name, "", Now );
		var identity = ledger.Find( "p" );
		Assert.Equal( AltIdentity.MaximumNames, identity.Names.Count );
		Assert.Equal( "name19", identity.Names[ 0 ] );
		Assert.Equal( AltIdentity.MaximumAddresses, identity.Addresses.Count );
		Assert.Equal( "net19", identity.Addresses[ 0 ].Address );
		ledger.Record( "later", "L", "later", "", "", Now + AltLedger.Retention + TimeSpan.FromDays( 1 ) );
		Assert.Null( ledger.Find( "p" ) );
	}

	[Fact]
	public void SuspectsStayFastWithAFullLedger()
	{
		var ledger = new AltLedger();
		for ( var player = 0; player < AltLedger.MaximumIdentities; player++ )
			ledger.Record( "p" + player, "n", "p" + player, "net" + player % 50, "party" + player % 30, Now );
		var timer = System.Diagnostics.Stopwatch.StartNew();
		var suspects = ledger.Suspects( _ => false );
		timer.Stop();
		Assert.Equal( AltLedger.MaximumIdentities, suspects.Count );
		Assert.True( timer.Elapsed < TimeSpan.FromSeconds( 2 ), $"Took {timer.Elapsed}." );
	}

	[Fact]
	public void AFullLedgerRoundTripsWithinOneStoredDocument()
	{
		var ledger = new AltLedger();
		for ( var player = 0; player < AltLedger.MaximumIdentities; player++ )
			for ( var visit = 0; visit < 6; visit++ )
				ledger.Record( "7656119800000" + player.ToString( "0000" ), "SomeLongPlayerName" + visit, "7656119800009999",
					"0123456789ab" + visit, "1099511627776000" + visit, Now + TimeSpan.FromMinutes( player ) );
		var state = AltCodec.WriteState( ledger.Identities );
		Assert.True( System.Text.Encoding.UTF8.GetByteCount( state ) < SboxSuperuser.Core.Storage.StoreFormat.MaximumBytes / 2 );
		var identities = AltCodec.ReadState( state );
		Assert.True( identities.Count > AltLedger.MaximumIdentities / 2 );
		Assert.Equal( ledger.Identities.Max( identity => identity.LastSeenUtc ), identities.Max( identity => identity.LastSeenUtc ) );
	}
}
