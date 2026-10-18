using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class PrivateReplyValidationTests
{
	[Fact]
	public void ValidRepliesRetainAllStructuredFields()
	{
		Assert.True( PrivateReplyValidation.IsValid( Guid.NewGuid(), CommandStatus.Success, "Done", new[] { "page" }, new[] { "1" } ) );
	}

	[Fact]
	public void InvalidIdentityStatusOrFieldShapeRejects()
	{
		var id = Guid.NewGuid();
		Assert.False( PrivateReplyValidation.IsValid( Guid.Empty, CommandStatus.Success, "Done", Array.Empty<string>(), Array.Empty<string>() ) );
		Assert.False( PrivateReplyValidation.IsValid( id, (CommandStatus)999, "Done", Array.Empty<string>(), Array.Empty<string>() ) );
		Assert.False( PrivateReplyValidation.IsValid( id, CommandStatus.Success, "Done", new[] { "same", "same" }, new[] { "a", "b" } ) );
		Assert.False( PrivateReplyValidation.IsValid( id, CommandStatus.Success, "Done", new[] { "a" }, Array.Empty<string>() ) );
		Assert.False( PrivateReplyValidation.IsValid( id, CommandStatus.Success, "Done", new string[] { null }, new[] { "a" } ) );
	}

	[Fact]
	public void PayloadBoundsRejectOversizedMessagesAndAggregateData()
	{
		var id = Guid.NewGuid();
		Assert.False( PrivateReplyValidation.IsValid( id, CommandStatus.Success, new string( 'x', 1025 ), Array.Empty<string>(), Array.Empty<string>() ) );
		Assert.False( PrivateReplyValidation.IsValid( id, CommandStatus.Success, "Done", Enumerable.Range( 0, 9 ).Select( i => "row" + i ).ToArray(),
			Enumerable.Repeat( new string( 'x', 1024 ), 9 ).ToArray() ) );
	}
}
