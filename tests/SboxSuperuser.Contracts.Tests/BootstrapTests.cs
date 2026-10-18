using SboxSuperuser.Core.Bootstrap;
using SboxSuperuser.Core.Identity;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class BootstrapTests
{
	[Fact]
	public void RepeatedRegistrationIsIdempotentAndDuplicatesFailClosed()
	{
		var registrations = new BootstrapRegistrations();
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		registrations.Attach( first, true );
		registrations.Attach( first, true );
		Assert.Equal( 1, registrations.Count );
		Assert.True( registrations.AllowsEditorTesting );
		registrations.Attach( second, true );
		Assert.Equal( 2, registrations.Count );
		Assert.False( registrations.AllowsEditorTesting );
		registrations.Detach( second );
		registrations.Detach( second );
		Assert.Equal( 1, registrations.Count );
		registrations.Clear();
		Assert.Equal( 0, registrations.Count );
	}

	[Fact]
	public void RegistrationUpdatesCannotRetainEditorOptIn()
	{
		var registrations = new BootstrapRegistrations();
		var id = Guid.NewGuid();
		registrations.Attach( id, true );
		registrations.Attach( id, false );
		Assert.False( registrations.AllowsEditorTesting );
	}

	[Theory]
	[InlineData( "{}" )]
	[InlineData( "{\"schemaVersion\":2,\"owners\":[]}" )]
	[InlineData( "{\"schemaVersion\":1,\"schemaVersion\":1,\"owners\":[]}" )]
	[InlineData( "{\"schemaVersion\":1,\"owners\":[],\"role\":\"owner\"}" )]
	[InlineData( "{\"schemaVersion\":1,\"owners\":[0]}" )]
	[InlineData( "{\"schemaVersion\":1,\"owners\":[\"0\"]}" )]
	[InlineData( "{\"schemaVersion\":1,\"owners\":[\"0123\"]}" )]
	[InlineData( "{\"schemaVersion\":1,\"owners\":[\"123\",\"123\"]}" )]
	[InlineData( "{\"schemaVersion\":1,\"owners\":[\"owner\"]}" )]
	public void OwnerConfigurationRejectsAmbiguousOrImplicitAuthority( string json )
	{
		Assert.Throws<ArgumentException>( () => OwnerConfiguration.Parse( json ) );
	}

	[Fact]
	public void OwnerMembershipIsExactAndAnEmptyListGrantsNobody()
	{
		var explicitOwners = OwnerConfiguration.Parse( "{\"schemaVersion\":1,\"owners\":[\"76561199542962865\"]}" );
		Assert.Equal( 1, explicitOwners.Count );
		Assert.True( explicitOwners.Contains( "76561199542962865" ) );
		Assert.False( explicitOwners.Contains( "76561199542962866" ) );
		Assert.False( explicitOwners.Contains( "dedicated-console" ) );
		var noOwners = OwnerConfiguration.Parse( "{\"schemaVersion\":1,\"owners\":[]}" );
		Assert.False( noOwners.Contains( "76561199542962865" ) );
	}

	[Fact]
	public void OwnerConfigurationHasBoundedInput()
	{
		Assert.Throws<ArgumentException>( () => OwnerConfiguration.Parse( new string( ' ', 65537 ) ) );
		var ids = string.Join( ",", Enumerable.Range( 1, 129 ).Select( id => $"\"{id}\"" ) );
		Assert.Throws<ArgumentException>( () => OwnerConfiguration.Parse( $"{{\"schemaVersion\":1,\"owners\":[{ids}]}}" ) );
	}
}
