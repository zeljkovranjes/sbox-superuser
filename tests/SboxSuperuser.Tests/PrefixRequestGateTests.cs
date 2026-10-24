using SboxSuperuser.Core.Prefixes;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class PrefixRequestGateTests
{
	[Fact]
	public void SnapshotRequestsAreLimitedPerConnection()
	{
		var gate = new PrefixRequestGate();
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		var now = DateTimeOffset.UtcNow;

		Assert.True( gate.TryEnter( first, now ) );
		Assert.False( gate.TryEnter( first, now.AddSeconds( 1 ) ) );
		Assert.True( gate.TryEnter( second, now.AddSeconds( 1 ) ) );
		Assert.True( gate.TryEnter( first, now.AddSeconds( 2 ) ) );
	}
}
