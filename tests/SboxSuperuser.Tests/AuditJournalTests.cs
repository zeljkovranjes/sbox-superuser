using SboxSuperuser.Core.Audit;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class AuditJournalTests
{
	private static AuditRecord Record( long sequence, DateTimeOffset? created = null ) => new( sequence, Guid.NewGuid(), Guid.NewGuid(),
		"Player", "76561198000000001", "Chat", "test.run", null, null, created ?? DateTimeOffset.UtcNow );

	[Fact]
	public void UnknownIsDefaultAndCompletedOutcomeCannotBeRewritten()
	{
		var intent = Record( 1 );
		Assert.Equal( "Unknown", intent.Outcome );
		Assert.Null( intent.CompletedUtc );
		var done = intent.Complete( "Denied", intent.CreatedUtc.AddMinutes( -1 ) );
		Assert.Equal( intent.CreatedUtc, done.CompletedUtc );
		Assert.Throws<InvalidOperationException>( () => done.Complete( "Success", DateTimeOffset.UtcNow ) );
	}

	[Fact]
	public void RetentionProtectsActiveIntentsAndRefusesCapacityOverflow()
	{
		var active = Record( 1, DateTimeOffset.UtcNow.AddDays( -100 ) );
		var recent = Record( 2 );
		var journal = new AuditJournal( 1 ).Append( active ).Append( recent );
		Assert.Equal( active.Id, Assert.Single( journal.Trim( DateTimeOffset.UtcNow, new HashSet<Guid> { active.Id } ).Records ).Id );
		Assert.Throws<InvalidOperationException>( () => journal.Trim( DateTimeOffset.UtcNow, new HashSet<Guid> { active.Id, recent.Id } ) );
	}

	[Fact]
	public void StrictFormatRoundTripsCheckpointAndRejectsDuplicateRecords()
	{
		var record = Record( 1 );
		var journal = new AuditJournal().Append( record ).WithRetentionCheckpoint( true );
		var payload = "{\"state\":" + AuditFormat.WriteState( journal ) + "}";
		Assert.True( AuditFormat.ReadPayload( payload ).NeedsRetentionCheckpoint );
		Assert.Throws<ArgumentException>( () => new AuditJournal( records: new[] { record, record }, nextSequence: 2 ) );
		Assert.Throws<FormatException>( () => AuditFormat.ReadPayload( payload.Replace( "\"auditSchema\":1", "\"auditSchema\":1,\"extra\":0" ) ) );
	}
}
