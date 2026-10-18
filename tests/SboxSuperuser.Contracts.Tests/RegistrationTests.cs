using SboxSuperuser.Engine.Commands;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class RegistrationTests
{
	private static readonly CommandHandler Handler = ( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Executed" ) );
	private static CommandRegistry Registry( Func<bool> available = null ) => new( new[] { "test.use" }, _ => { }, available ?? (() => true) );
	private static CommandDescriptor Descriptor( string module = "test", string name = "test.run", string[] aliases = null,
		IEnumerable<CommandArgumentDescriptor> arguments = null, string permission = "test.use" )
		=> new( name, "Test command", permission, module, arguments, aliases );

	[Fact]
	public void CommitPublishesCanonicalAndAliasOnceAndDisposalRevokesBoth()
	{
		var registry = Registry();
		using var scope = new CommandRegistrationScope( registry, "test" );
		scope.RegisterCommand( Descriptor( aliases: new[] { "test.alias" } ), Handler );
		Assert.Null( registry.Find( "test.run" ) );
		scope.Commit();
		Assert.Same( registry.Find( "test.run" ), registry.Find( "test.alias" ) );
		Assert.Single( registry.Descriptors );
		scope.Dispose();
		Assert.Empty( registry.Descriptors );
		Assert.Null( registry.Find( "test.alias" ) );
	}

	[Fact]
	public void ReplacementChangesHandlerIdentityAndOldDisposeCannotRemoveIt()
	{
		var registry = Registry();
		using var first = new CommandRegistrationScope( registry, "test" );
		first.RegisterCommand( Descriptor(), Handler );
		first.Commit();
		var old = registry.Find( "test.run" );
		using var second = new CommandRegistrationScope( registry, "test" );
		second.RegisterCommand( Descriptor( aliases: new[] { "new.alias" } ), Handler );
		second.Commit();
		first.Dispose();
		Assert.NotEqual( old.RegistrationId, registry.Find( "test.run" ).RegistrationId );
		Assert.NotNull( registry.Find( "new.alias" ) );
	}

	[Theory]
	[InlineData( "test.run", "other.alias" )]
	[InlineData( "other.run", "test.alias" )]
	[InlineData( "test.alias", "other.alias" )]
	[InlineData( "other.run", "test.run" )]
	public void AnyCrossModuleNameCollisionRollsBackWholeBatch( string name, string alias )
	{
		var registry = Registry();
		using var first = new CommandRegistrationScope( registry, "test" );
		first.RegisterCommand( Descriptor( aliases: new[] { "test.alias" } ), Handler );
		first.Commit();
		using var second = new CommandRegistrationScope( registry, "other" );
		second.RegisterCommand( Descriptor( "other", "new.command" ), Handler );
		second.RegisterCommand( Descriptor( "other", name, new[] { alias } ), Handler );
		Assert.Throws<ArgumentException>( second.Commit );
		Assert.Null( registry.Find( "new.command" ) );
		Assert.Single( registry.Descriptors );
	}

	[Fact]
	public void DuplicateAliasAndWrongModuleAndUnknownPermissionsAreRejected()
	{
		var registry = Registry();
		foreach ( var descriptor in new[]
		{
			Descriptor( aliases: new[] { "test.run" } ), Descriptor( "impostor" ), Descriptor( permission: "unknown.use" )
		} )
		{
			using var scope = new CommandRegistrationScope( registry, "test" );
			scope.RegisterCommand( descriptor, Handler );
			Assert.Throws<ArgumentException>( scope.Commit );
			Assert.Empty( registry.Descriptors );
		}
	}

	[Fact]
	public void InvalidReplacementPreservesOldHandlerAndPermissionSet()
	{
		string[] published = Array.Empty<string>();
		var registry = new CommandRegistry( new[] { "test.use" }, keys => published = keys.ToArray(), () => true );
		using var first = new CommandRegistrationScope( registry, "test" );
		first.RegisterPermission( new PermissionDescriptor( "test.extra", "Extra" ) );
		first.RegisterCommand( Descriptor(), Handler );
		first.Commit();
		var old = registry.Find( "test.run" );
		using var second = new CommandRegistrationScope( registry, "test" );
		second.RegisterCommand( Descriptor( permission: "missing.key" ), Handler );
		Assert.Throws<ArgumentException>( second.Commit );
		Assert.Same( old, registry.Find( "test.run" ) );
		Assert.Contains( "test.extra", published );
	}

	[Fact]
	public void RevokedRuntimeCannotActivateStagedCommands()
	{
		var active = true;
		var registry = Registry( () => active );
		using var scope = new CommandRegistrationScope( registry, "test" );
		scope.RegisterCommand( Descriptor(), Handler );
		active = false;
		Assert.Throws<InvalidOperationException>( scope.Commit );
		Assert.Empty( registry.Descriptors );
	}

	[Fact]
	public void DiscoveryFailureRemovesAllPartialDeclarations()
	{
		var registry = Registry();
		using var scope = new CommandRegistrationScope( registry, "test", staged =>
		{
			staged.RegisterCommand( Descriptor(), Handler );
			throw new ArgumentException( "Invalid later method" );
		} );
		Assert.Throws<ArgumentException>( scope.DiscoverCommands );
		scope.Commit();
		Assert.Empty( registry.Descriptors );
	}

	[Fact]
	public void PermissionOwnerRemovalAlsoRevokesDependentCommands()
	{
		var registry = Registry();
		using var permissionOwner = new CommandRegistrationScope( registry, "permissions" );
		permissionOwner.RegisterPermission( new PermissionDescriptor( "custom.use", "Custom permission" ) );
		permissionOwner.Commit();
		using var dependent = new CommandRegistrationScope( registry, "test" );
		dependent.RegisterCommand( Descriptor( permission: "custom.use" ), Handler );
		dependent.Commit();
		permissionOwner.Dispose();
		Assert.Empty( registry.Descriptors );
	}

	[Fact]
	public void InvalidArgumentShapesNeverEnterRegistry()
	{
		var invalid = new[]
		{
			new[] { new CommandArgumentDescriptor( "first", "First", CommandArgumentKind.String, true ), new CommandArgumentDescriptor( "second", "Second", CommandArgumentKind.String ) },
			new[] { new CommandArgumentDescriptor( "text", "Text", CommandArgumentKind.Integer, isRemainder: true ) },
			new[] { new CommandArgumentDescriptor( "text", "Text", CommandArgumentKind.String, minimum: 0 ) },
			new[] { new CommandArgumentDescriptor( "enum", "Enum", CommandArgumentKind.Enum, choices: new[] { "a", "A" } ) },
			new[] { new CommandArgumentDescriptor( "enum", "Enum", CommandArgumentKind.Enum ) },
			new[] { new CommandArgumentDescriptor( "target", "Target", CommandArgumentKind.Player ) },
			new[] { new CommandArgumentDescriptor( "text", "Text", CommandArgumentKind.String, targetPolicy: CommandTargetPolicy.AnyPlayer ) }
		};
		foreach ( var arguments in invalid )
		{
			Assert.Throws<ArgumentException>( () => CommandMetadata.Validate( Descriptor( arguments: arguments ) ) );
		}
	}

	[Fact]
	public void ReservedPermissionAndModuleCannotBeReplaced()
	{
		var registry = Registry();
		using var reserved = new CommandRegistrationScope( registry, "superuser.core" );
		Assert.Throws<ArgumentException>( reserved.Commit );
		using var scope = new CommandRegistrationScope( registry, "test" );
		scope.RegisterPermission( new PermissionDescriptor( "test.use", "Steal" ) );
		Assert.Throws<ArgumentException>( scope.Commit );
	}
}
