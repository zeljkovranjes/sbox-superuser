using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ReplySecurityTests
{
	[Theory]
	[InlineData( "forged\nline" )]
	[InlineData( "forged\rline" )]
	[InlineData( "forged\tline" )]
	public void CommandResultsRejectControlCharacterInjection( string payload )
	{
		Assert.Throws<ArgumentException>( () => new CommandResult( CommandStatus.Success, payload ) );
		Assert.Throws<ArgumentException>( () => new CommandResult( CommandStatus.Success, "Done",
			new Dictionary<string, string> { [ "row1" ] = payload } ) );
		Assert.Throws<ArgumentException>( () => new CommandResult( CommandStatus.Success, "Done",
			new Dictionary<string, string> { [ payload ] = "value" } ) );
		Assert.False( PrivateReplyValidation.IsValid( Guid.NewGuid(), CommandStatus.Success,
			payload, Array.Empty<string>(), Array.Empty<string>() ) );
		Assert.False( PrivateReplyValidation.IsValid( Guid.NewGuid(), CommandStatus.Success,
			"Done", new[] { "row1" }, new[] { payload } ) );
		Assert.False( PrivateReplyValidation.IsValid( Guid.NewGuid(), CommandStatus.Success,
			"Done", new[] { payload }, new[] { "value" } ) );
	}
}
