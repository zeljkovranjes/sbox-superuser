using SboxSuperuser.Core.Commands;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class ConsoleRootStateTests
{
	private static readonly string[] Native = { "admin", "superuser", "superuser_request" };

	[Fact]
	public void ManagedRootAndTransportCollisionsFailClosed()
	{
		Assert.True( new ConsoleRootState( "admin", Native ).IsAvailable );
		Assert.False( new ConsoleRootState( "admin", Native.Append( "ADMIN" ) ).IsAvailable );
		Assert.False( new ConsoleRootState( "admin", Native.Append( "superuser_request" ) ).IsAvailable );
		Assert.False( new ConsoleRootState( "admin", new[] { "admin" } ).IsAvailable );
	}

	[Fact]
	public void ConfiguredLogicalRootUsesFallbackAndRejectsExistingName()
	{
		var alternate = new ConsoleRootState( "operators", Native.Append( "admin" ) );
		Assert.True( alternate.IsAvailable );
		Assert.Contains( "superuser operators", alternate.Message );
		Assert.False( new ConsoleRootState( "operators", Native.Append( "operators" ) ).IsAvailable );
		Assert.False( new ConsoleRootState( "/invalid", Native ).IsAvailable );
	}
}
