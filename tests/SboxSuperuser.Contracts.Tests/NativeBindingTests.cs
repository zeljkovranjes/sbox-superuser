using SboxSuperuser.Engine.NativeSettings;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class NativeBindingTests
{
	private static NativeSettingDescriptor Number( string nativeName = "game_capacity" ) => new(
		"capacity", nativeName, "Player capacity", "game.capacity", CommandArgumentKind.Integer, "8",
		NativePersistence.Framework, minimum: 1, maximum: 32 );

	private static AuditIntent[] Intents() => new[]
	{
		new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), ActorKind.InternalService, "test.settings",
			CommandOrigin.InternalService, "settings.set.capacity", DateTimeOffset.UtcNow )
	};

	[Theory]
	[InlineData( "game_capacity;quit" )]
	[InlineData( "game_capacity 3" )]
	[InlineData( "game_capacity\nquit" )]
	[InlineData( "GameCapacity" )]
	public void NativeNameCannotContainConsoleSyntax( string name ) => Assert.Throws<ArgumentException>( () => Number( name ) );

	[Theory]
	[InlineData( "0" )]
	[InlineData( "33" )]
	[InlineData( "1.5" )]
	[InlineData( "NaN" )]
	[InlineData( "9223372036854775808" )]
	public void InvalidSettingValuesNeverNormalize( string value ) => Assert.Null( NativeValue.Normalize( Number(), value ) );

	[Fact]
	public void ResetDefaultMustRespectDeclaredConstraints() => Assert.Throws<ArgumentException>( () => new NativeSettingDescriptor(
		"capacity", "game_capacity", "Player capacity", "game.capacity", CommandArgumentKind.Integer, "0", minimum: 1 ) );

	[Fact]
	public void PendingStateCannotCompeteWithEnginePersistence() => Assert.Throws<ArgumentException>( () => new NativeSettingDescriptor(
		"capacity", "game_capacity", "Player capacity", "game.capacity", CommandArgumentKind.Integer, "8", NativePersistence.Engine, requiresRestart: true ) );

	[Fact]
	public void CanonicalValuesAndExplicitEnumChoicesRoundTrip()
	{
		Assert.Equal( "8", NativeValue.Normalize( Number(), "008" ) );
		var choice = new NativeSettingDescriptor( "mode", "game_mode", "Mode", "game.mode", CommandArgumentKind.Enum,
			"friendly", choices: new[] { "friendly", "strict" } );
		Assert.Equal( "strict", NativeValue.Normalize( choice, "STRICT" ) );
		Assert.Null( NativeValue.Normalize( choice, "unknown" ) );
	}

	[Fact]
	public void PersistedValueRequiresTheSameNativeContract()
	{
		var binding = Number();
		var transaction = new PolicyTransaction( NativeSettingState.Serialize( binding, "12" ), Intents() );
		var stored = new StoreReadResult( StoreReadStatus.Found, transaction.CreateWriteRequest( new StoreKey( "test", "setting" ), 0 ).Document );
		Assert.Equal( new NativeSettingState( 1, "12" ), NativeSettingState.Read( binding, stored ) );
		Assert.Throws<InvalidOperationException>( () => NativeSettingState.Read( Number( "different_native" ), stored ) );
	}

	[Fact]
	public void CorruptionCannotBecomeFirstRunDefaults()
	{
		Assert.Equal( new NativeSettingState( 0, null ), NativeSettingState.Read( Number(), new StoreReadResult( StoreReadStatus.Missing ) ) );
		Assert.Throws<InvalidOperationException>( () => NativeSettingState.Read( Number(), new StoreReadResult( StoreReadStatus.Corrupt ) ) );
		var transaction = new PolicyTransaction( NativeSettingState.Serialize( Number(), "999" ), Intents() );
		var stored = new StoreReadResult( StoreReadStatus.Found, transaction.CreateWriteRequest( new StoreKey( "test", "setting" ), 0 ).Document );
		Assert.Throws<InvalidOperationException>( () => NativeSettingState.Read( Number(), stored ) );
	}
}
