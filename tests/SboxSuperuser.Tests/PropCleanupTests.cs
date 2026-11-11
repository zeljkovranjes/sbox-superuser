using SboxSuperuser.Core.Props;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class PropCleanupTests
{
	[Fact]
	public void DelaysOutsideTenSecondsToSevenDaysAreRefusedAndPermanentIsAllowed()
	{
		Assert.Null( PropCleanup.Problem( null ) );
		Assert.Null( PropCleanup.Problem( TimeSpan.FromSeconds( 10 ) ) );
		Assert.Null( PropCleanup.Problem( TimeSpan.FromDays( 7 ) ) );
		Assert.NotNull( PropCleanup.Problem( TimeSpan.FromSeconds( 9 ) ) );
		Assert.NotNull( PropCleanup.Problem( TimeSpan.FromDays( 8 ) ) );
	}

	[Fact]
	public void CodecRoundTripsDelaysAndPermanent()
	{
		Assert.Equal( TimeSpan.FromMinutes( 5 ), PropCleanupCodec.ReadState( PropCleanupCodec.WriteState( new PropCleanup( TimeSpan.FromMinutes( 5 ) ) ) ).Delay );
		Assert.Null( PropCleanupCodec.ReadState( PropCleanupCodec.WriteState( new PropCleanup( null ) ) ).Delay );
		Assert.Throws<FormatException>( () => PropCleanupCodec.ReadState( "{\"schema\":1,\"delaySeconds\":1}" ) );
	}
}
