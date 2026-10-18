using SboxSuperuser.Core.Moderation;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class ModerationLedgerTests
{
	private static readonly DateTimeOffset Now = new( 2026, 10, 18, 0, 0, 0, TimeSpan.Zero );
	private const string Target = "76561198000000001";
	private static ModerationEntry Entry( ModerationKind kind, DateTimeOffset? expires = null, Guid? revokes = null, DateTimeOffset? created = null )
		=> new( Guid.NewGuid(), Guid.NewGuid(), Target, kind, "DedicatedConsole", "dedicated-console", "Console", "Test reason", created ?? Now, expires, revokes );

	[Theory]
	[InlineData( "Ban" )]
	[InlineData( "TextMute" )]
	public void ExpiryUsesTheOriginalUtcTimestampAfterReload( string kindName )
	{
		var kind = Enum.Parse<ModerationKind>( kindName );
		var entry = Entry( kind, Now.AddHours( 2 ) );
		var ledger = new ModerationLedger( 1, new[] { entry } );
		var reloaded = ModerationCodec.Read( "{\"state\":" + ModerationCodec.WriteState( ledger ) + "}", 1 );
		Assert.NotNull( reloaded.Active( Target, kind, Now.AddHours( 1 ) ) );
		Assert.Null( reloaded.Active( Target, kind, Now.AddHours( 2 ) ) );
		Assert.Equal( entry.ExpiresUtc, reloaded.Entries.Single().ExpiresUtc );
		Assert.Single( reloaded.Entries );
	}

	[Fact]
	public void RevocationLinksTheOriginalWithoutErasingHistory()
	{
		var ban = Entry( ModerationKind.Ban );
		var unban = Entry( ModerationKind.Unban, revokes: ban.Id );
		var ledger = new ModerationLedger( 2, new[] { ban, unban } );
		Assert.Null( ledger.Active( Target, ModerationKind.Ban, Now ) );
		Assert.Equal( ban.Id, ledger.Entries[ 1 ].Revokes );
		Assert.Equal( 2, ledger.Entries.Count );
		Assert.Throws<ArgumentException>( () => ledger.Append( Entry( ModerationKind.Unban, revokes: ban.Id ) ) );
	}

	[Fact]
	public void WrongOrMissingRevocationCannotClearABan()
	{
		var ban = Entry( ModerationKind.Ban );
		Assert.Throws<ArgumentException>( () => new ModerationLedger( 2, new[] { ban, Entry( ModerationKind.Unban, revokes: Guid.NewGuid() ) } ) );
		Assert.Throws<ArgumentException>( () => Entry( ModerationKind.Unban ) );
		Assert.Throws<ArgumentException>( () => new ModerationLedger( 2, new[] { ban, Entry( ModerationKind.Unmute, revokes: ban.Id ) } ) );
	}

	[Fact]
	public void ActiveRestrictionCannotBeSilentlyShortenedOrReplaced()
	{
		var ledger = new ModerationLedger( 1, new[] { Entry( ModerationKind.Ban ) } );
		Assert.Throws<ArgumentException>( () => ledger.Append( Entry( ModerationKind.Ban, Now.AddMinutes( 1 ) ) ) );
	}

	[Fact]
	public void ExpiredRestrictionCanBeRenewedWithHistoryRetained()
	{
		var old = Entry( ModerationKind.Ban, Now.AddMinutes( 1 ) );
		var renewal = Entry( ModerationKind.Ban, created: Now.AddMinutes( 2 ) );
		var ledger = new ModerationLedger( 2, new[] { old, renewal } );
		Assert.Equal( renewal.Id, ledger.Active( Target, ModerationKind.Ban, Now.AddMinutes( 3 ) ).Id );
		Assert.Equal( 2, ledger.Entries.Count );
	}

	[Fact]
	public void WarningsDoNotEscalateIntoRestrictions()
	{
		var ledger = new ModerationLedger( 3, Enumerable.Range( 0, 3 ).Select( _ => Entry( ModerationKind.Warn ) ) );
		Assert.Null( ledger.Active( Target, ModerationKind.Ban, Now ) );
		Assert.Null( ledger.Active( Target, ModerationKind.TextMute, Now ) );
	}

	[Fact]
	public void FullHistoryStillHasRoomToRevokeEveryActiveRestriction()
	{
		var ban = Entry( ModerationKind.Ban );
		var history = new[] { ban }.Concat( Enumerable.Range( 0, ModerationLedger.MaximumRecords - 2 ).Select( _ => Entry( ModerationKind.Warn ) ) );
		var ledger = new ModerationLedger( 1, history );
		Assert.Throws<ArgumentException>( () => ledger.Append( Entry( ModerationKind.Warn ) ) );
		var revoked = ledger.Append( Entry( ModerationKind.Unban, revokes: ban.Id ) );
		Assert.Equal( ModerationLedger.MaximumRecords, revoked.Entries.Count );
		Assert.Null( revoked.Active( Target, ModerationKind.Ban, Now ) );
	}

	[Fact]
	public void NonpositiveExpiryAndNonUtcTimesAreRejected()
	{
		Assert.Throws<ArgumentException>( () => Entry( ModerationKind.Ban, Now ) );
		Assert.Throws<ArgumentException>( () => Entry( ModerationKind.Ban, Now.AddSeconds( -1 ) ) );
		Assert.Throws<ArgumentException>( () => Entry( ModerationKind.Warn, created: Now.ToOffset( TimeSpan.FromHours( 1 ) ) ) );
	}

	[Fact]
	public void EncodedByteLimitStillReservesAFullRevocationReason()
	{
		var ban = Entry( ModerationKind.Ban );
		var ledger = new ModerationLedger( 1, new[] { ban } );
		var reason = new string( '\u4e00', 256 );
		var reachedLimit = false;
		for ( var index = 0; index < ModerationLedger.MaximumRecords; index++ )
		{
			var note = new ModerationEntry( Guid.NewGuid(), Guid.NewGuid(), Target, ModerationKind.Warn,
				"DedicatedConsole", "dedicated-console", "Console", reason, Now );
			var candidate = ledger.Append( note );
			try { ModerationCodec.WriteState( candidate ); }
			catch ( InvalidOperationException ) { reachedLimit = true; break; }
			ledger = candidate;
		}
		Assert.True( reachedLimit );
		var revocation = new ModerationEntry( Guid.NewGuid(), Guid.NewGuid(), Target, ModerationKind.Unban,
			"DedicatedConsole", "dedicated-console", "Console", reason, Now, revokes: ban.Id );
		var revoked = ledger.Append( revocation );
		Assert.NotEmpty( ModerationCodec.WriteState( revoked ) );
		Assert.Null( revoked.Active( Target, ModerationKind.Ban, Now ) );
	}
}
