using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ContractTests
{
	[Theory]
	[InlineData( "admin.*" )]
	[InlineData( "Admin.kick" )]
	[InlineData( "admin..kick" )]
	[InlineData( "admin." )]
	[InlineData( "../owners" )]
	[InlineData( "" )]
	public void PermissionKeysRejectWildcardsAndMalformedNames( string key )
	{
		Assert.Throws<ArgumentException>( () => new PermissionDescriptor( key, "Help" ) );
	}

	[Theory]
	[InlineData( "0" )]
	[InlineData( "0123" )]
	[InlineData( "-1" )]
	[InlineData( " 123" )]
	[InlineData( "18446744073709551616" )]
	public void PlayerIdsRejectAmbiguousOrInvalidValues( string value )
	{
		Assert.Throws<ArgumentException>( () => new PlayerReference( value ) );
	}

	[Fact]
	public void MutableRegistrationInputsCannotChangeDescriptors()
	{
		var choices = new[] { "one", "two" };
		var argument = new CommandArgumentDescriptor( "choice", "Choice", CommandArgumentKind.Enum, choices: choices );
		var arguments = new[] { argument };
		var aliases = new[] { "alternate" };
		var descriptor = new CommandDescriptor( "sample", "Help", "sample.run", "sample", arguments, aliases );
		choices[ 0 ] = "changed";
		arguments[ 0 ] = null;
		aliases[ 0 ] = "changed";
		Assert.Equal( "one", descriptor.Arguments[ 0 ].Choices[ 0 ] );
		Assert.Equal( "alternate", descriptor.Aliases[ 0 ] );
		Assert.Throws<NotSupportedException>( () => ((IList<string>)descriptor.Aliases)[ 0 ] = "changed" );
	}

	[Fact]
	public void RequestPayloadCannotCarryAuthorityAndHostContextHasNoPublicConstructor()
	{
		Assert.Equal( new[] { "Arguments", "Command", "RequestId" },
			typeof( CommandRequest ).GetProperties().Select( property => property.Name ).OrderBy( name => name ) );
		Assert.Empty( typeof( CommandContext ).GetConstructors() );
		Assert.Empty( typeof( ActorIdentity ).GetConstructors() );
		Assert.Throws<ArgumentException>( () => new ActorIdentity( ActorKind.Unknown, "anything" ) );
		Assert.Throws<ArgumentException>( () => new ActorIdentity( ActorKind.DedicatedConsole, "0" ) );
	}

	[Fact]
	public void TypedArgumentsSnapshotAndRejectDefaultsAndMutableValues()
	{
		var source = new Dictionary<string, object> { [ "count" ] = 3L };
		var arguments = new CommandArguments( source );
		source[ "count" ] = 4L;
		Assert.Equal( 3L, arguments.Get<long>( "count" ) );
		Assert.False( arguments.Contains( "missing" ) );
		Assert.Throws<ArgumentException>( () => arguments.Get<long>( "missing" ) );
		Assert.Throws<ArgumentException>( () => arguments.Get<string>( "count" ) );
		Assert.Throws<ArgumentException>( () => new CommandArguments( new Dictionary<string, object> { [ "bad" ] = new List<int>() } ) );
		Assert.Throws<ArgumentException>( () => new CommandArguments( new Dictionary<string, object> { [ "bad" ] = double.NaN } ) );
	}

	[Fact]
	public void PermanentDurationIsExplicitAndFiniteDurationMustBePositive()
	{
		Assert.True( CommandDuration.Permanent.IsPermanent );
		Assert.Null( CommandDuration.Permanent.Value );
		Assert.False( new CommandDuration( TimeSpan.FromMinutes( 5 ) ).IsPermanent );
		Assert.Throws<ArgumentOutOfRangeException>( () => new CommandDuration( TimeSpan.Zero ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => new CommandDuration( TimeSpan.FromTicks( -1 ) ) );
	}

	[Fact]
	public void UnknownResultsAndStorageFailuresCannotClaimConfirmedSuccess()
	{
		Assert.Equal( CommandStatus.Unknown, default );
		Assert.Equal( StoreReadStatus.Unknown, default );
		Assert.Equal( StoreWriteStatus.Unknown, default );
		Assert.Throws<ArgumentException>( () => new StoreWriteResult( StoreWriteStatus.Committed ) );
		Assert.Throws<ArgumentException>( () => new StoreWriteResult( StoreWriteStatus.Failed, 1 ) );
		Assert.Throws<ArgumentException>( () => new StoreWriteResult( StoreWriteStatus.Committed, 0 ) );
		Assert.Null( new StoreWriteResult( StoreWriteStatus.Unknown ).CommittedRevision );
		Assert.NotEqual( StoreReadStatus.Missing, StoreReadStatus.Corrupt );
		Assert.NotEqual( StoreReadStatus.Missing, StoreReadStatus.UnsupportedSchema );
	}

	[Fact]
	public void ReadOutcomesRequireDocumentOnlyForFound()
	{
		var document = new VersionedStoreDocument( 1, 1, "{}" );
		Assert.Throws<ArgumentException>( () => new StoreReadResult( StoreReadStatus.Found ) );
		Assert.Throws<ArgumentException>( () => new StoreReadResult( StoreReadStatus.Corrupt, document ) );
		Assert.Same( document, new StoreReadResult( StoreReadStatus.Found, document ).Document );
	}

	[Fact]
	public void StoreWritesRequireNextRevisionAndLogicalKeys()
	{
		var key = new StoreKey( "security", "policy" );
		var document = new VersionedStoreDocument( 2, 1, "{}" );
		Assert.Equal( 0, new StoreWriteRequest( key, document, 0 ).ExpectedRevision );
		Assert.Throws<ArgumentException>( () => new StoreWriteRequest( key, document, 1 ) );
		Assert.Throws<ArgumentException>( () => new StoreWriteRequest( key, document, long.MaxValue ) );
		Assert.Throws<ArgumentException>( () => new StoreKey( "security", "../owners" ) );
		Assert.Throws<ArgumentException>( () => new VersionedStoreDocument( 0, 1, "{}" ) );
	}

	[Fact]
	public void ModuleDependenciesAreSnapshotsAndApiVersionsAreExplicit()
	{
		var dependencies = new[] { new ModuleDependency( "security", 1 ) };
		var module = new ModuleDescriptor( "moderation", "Help", 1, dependencies );
		dependencies[ 0 ] = new ModuleDependency( "other", 2 );
		Assert.Equal( "security", module.Dependencies[ 0 ].ModuleId );
		Assert.Throws<ArgumentOutOfRangeException>( () => new ModuleDependency( "security", 0 ) );
	}

	[Fact]
	public void ResultDataCannotBeChangedAfterConstruction()
	{
		var fields = new Dictionary<string, string> { [ "code" ] = "safe" };
		var result = new CommandResult( CommandStatus.Unsupported, "Unavailable", fields );
		fields[ "code" ] = "changed";
		Assert.Equal( "safe", result.Data[ "code" ] );
	}
}
