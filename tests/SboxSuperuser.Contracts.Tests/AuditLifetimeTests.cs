using SboxSuperuser.Engine.Audit;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AuditLifetimeTests
{
	[Fact]
	public async Task FailedCompletionAdmissionReleasesRetentionPin()
	{
		var available = true;
		var audit = new HostAuditService( new AuditStoreHarness(), () => available, _ => { } );
		await audit.ConfigureAsync( () => DispatcherRig.Owner, new AuditRetentionOptions( 1 ), CancellationToken.None );
		AuditAction Action() => new( DispatcherRig.Owner, CommandOrigin.Console, Guid.NewGuid(), "test.run", DateTimeOffset.UtcNow );
		var first = Action();
		Assert.True( await audit.BeginAsync( first, CancellationToken.None ) );
		available = false;
		Assert.False( await audit.CompleteAsync( first, CommandStatus.Success, CancellationToken.None ) );
		available = true;
		Assert.True( await audit.BeginAsync( Action(), CancellationToken.None ) );
	}
}
