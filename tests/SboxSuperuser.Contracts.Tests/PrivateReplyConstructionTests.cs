using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class PrivateReplyConstructionTests
{
	[Fact]
	public void ReceiverRejectsWhitespaceThatCannotConstructAResult()
	{
		Assert.False( PrivateReplyValidation.IsValid( Guid.NewGuid(), CommandStatus.Success, " ", Array.Empty<string>(), Array.Empty<string>() ) );
		Assert.False( PrivateReplyValidation.IsValid( Guid.NewGuid(), CommandStatus.Success, "Done", new[] { " " }, new[] { "value" } ) );
	}
}
