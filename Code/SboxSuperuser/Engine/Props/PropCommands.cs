using SboxSuperuser.Core.Props;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Engine.Props;

/// <summary>Sharing and cleanup commands for prop protection, in their own module.</summary>
internal sealed class PropCommands : IDisposable
{
	internal const string Module = "superuser.props";
	internal const string TouchAllPermission = "admin.props.touchall";
	internal const string CleanupPermission = "admin.props.cleanup";

	private readonly CommandRegistrationScope _scope;
	private readonly PropOwnership _props;
	private readonly IPropWorld _world;
	private readonly Func<CommandContext, string, bool> _canUse;
	private readonly Func<string, string> _playerName;

	internal PropCommands( CommandRegistry registry, PropOwnership props, IPropWorld world, Func<CommandContext, string, bool> canUse,
		Func<string, string> playerName = null )
	{
		_props = props;
		_world = world;
		_canUse = canUse;
		_playerName = playerName ?? (id => id);
		_scope = new CommandRegistrationScope( registry, Module, managed: true );
		Register( "props.share", "Let a player touch everything you spawn", "admin.props.share", new[] { Player() },
			(context, arguments) => Share( context, arguments.Get<PlayerReference>( "user" ).SteamId, true ) );
		Register( "props.unshare", "Stop a player touching what you spawn", "admin.props.share", new[] { Player() },
			(context, arguments) => Share( context, arguments.Get<PlayerReference>( "user" ).SteamId, false ) );
		Register( "props.cleanup", "Remove your props, or with admin.props.cleanup another player's", "admin.props.cleanupown",
			new[] { new CommandArgumentDescriptor( "user", "Exact Steam ID or unique online name; yourself when omitted", CommandArgumentKind.Player,
				isOptional: true, targetPolicy: CommandTargetPolicy.AnyPlayer, audit: CommandArgumentAudit.SafeValue ) },
			(context, arguments) => Cleanup( context, arguments.Contains( "user" ) ? arguments.Get<PlayerReference>( "user" ).SteamId : null ) );
		Register( "props.cleanupall", "Remove every player's props; the map's own objects stay", "admin.props.cleanupall",
			Array.Empty<CommandArgumentDescriptor>(), (_, _) => Removed( _props.All, "everyone" ) );
		_scope.Commit();
	}

	private void Register( string name, string description, string permission, CommandArgumentDescriptor[] arguments, Func<CommandContext, CommandArguments, CommandResult> handler ) =>
		_scope.RegisterCommand( new CommandDescriptor( name, description, permission, Module, arguments, executionPolicy: CommandExecutionPolicy.Mutation ),
			(context, values) => Task.FromResult( handler( context, values ) ) );

	private CommandResult Share( CommandContext context, string friend, bool share )
	{
		if ( Caller( context ) is not { } owner ) return new CommandResult( CommandStatus.Unsupported, "Only a player in the game owns props." );
		var changed = share ? _props.Share( owner, friend ) : _props.Unshare( owner, friend );
		var name = _playerName( friend );
		return changed ? new CommandResult( CommandStatus.Success, share ? $"{name} can now touch your props." : $"{name} can no longer touch your props." )
			: new CommandResult( CommandStatus.NoChange, share ? $"{name} could already touch your props." : $"{name} couldn't touch your props." );
	}

	private CommandResult Cleanup( CommandContext context, string target )
	{
		var caller = Caller( context );
		target ??= caller;
		if ( target is null ) return new CommandResult( CommandStatus.InvalidArguments, "Name a player to clean up after." );
		if ( target != caller && !_canUse( context, CleanupPermission ) )
			return new CommandResult( CommandStatus.Denied, $"Cleaning up another player's props needs {CleanupPermission}." );
		return Removed( _props.ObjectsOf( target ), target == caller ? "you" : _playerName( target ) );
	}

	private CommandResult Removed( IReadOnlyList<Guid> objects, string whose )
	{
		var removed = _world.Remove( objects );
		foreach ( var id in objects ) _props.Forget( id );
		return new CommandResult( removed == 0 ? CommandStatus.NoChange : CommandStatus.Success, $"Removed {removed} props spawned by {whose}." );
	}

	private static string Caller( CommandContext context ) => context.Services is CommandExecutionServices services
		&& services.Authenticate() is { Kind: ActorKind.Player } actor ? actor.SubjectId : null;

	private static CommandArgumentDescriptor Player() => new( "user", "Exact Steam ID or unique online name", CommandArgumentKind.Player,
		targetPolicy: CommandTargetPolicy.AnyPlayer, audit: CommandArgumentAudit.SafeValue );

	public void Dispose() => _scope.Dispose();
}
