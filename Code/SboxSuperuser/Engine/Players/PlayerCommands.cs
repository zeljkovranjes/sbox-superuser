using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Engine.Players;

/// <summary>
/// Commands that act on players' bodies: punishments, movement, visibility and the map. Each one checks its own
/// permission and target protection through the dispatcher, then asks <see cref="IPlayerActions"/> to do the work.
/// </summary>
internal sealed partial class PlayerCommands : IDisposable
{
	internal const string Module = "superuser.players";
	private const int MaximumWhips = 50;
	private const int MaximumDamage = 1000000;
	private const int MaulBlows = 5;
	private const float MaulDamage = 20;
	private static readonly TimeSpan WhipInterval = TimeSpan.FromSeconds( 0.5 );

	private readonly CommandRegistrationScope _scope;
	private readonly Func<IPlayerActions> _actions;
	private readonly Func<string, string> _playerName;
	private readonly Action<string> _announce;
	private readonly Func<TimeSpan, Task> _wait;
	private readonly Core.Messages.MessageCatalog _messages;
	private readonly Func<string, bool> _mapAllowed;
	private readonly Func<IReadOnlyList<string>> _online;
	private readonly Func<CommandContext, string, bool> _canTarget;

	/// <param name="actions">Read on every command so a game can configure its adapter after the bootstrap starts.</param>
	/// <param name="wait">Delay between whip blows; the engine passes a main-thread delay.</param>
	/// <param name="canTarget">The dispatcher's target protection check, for commands that skip it when aimed at yourself.</param>
	internal PlayerCommands( CommandRegistry registry, Func<IPlayerActions> actions, Func<IReadOnlyList<string>> online,
		Func<CommandContext, string, bool> canTarget, Func<string, string> playerName = null, Action<string> announce = null, Func<TimeSpan, Task> wait = null,
		Core.Messages.MessageCatalog messages = null, Func<string, bool> mapAllowed = null )
	{
		_mapAllowed = mapAllowed;
		_messages = messages ?? new Core.Messages.MessageCatalog();
		_canTarget = canTarget;
		_actions = actions;
		_online = online;
		_playerName = playerName ?? (id => id);
		_announce = announce;
		_wait = wait ?? Task.Delay;
		_scope = new CommandRegistrationScope( registry, Module, managed: true );
		RegisterPunishments();
		RegisterStates();
		RegisterMovement();
		RegisterWorld();
		_scope.Commit();
	}

	private void Register( string name, string description, CommandArgumentDescriptor[] arguments, CommandHandler handler )
	{
		var descriptor = new CommandDescriptor( name, description, Permission( name ), Module, arguments,
			executionPolicy: CommandExecutionPolicy.Mutation );
		_scope.RegisterCommand( descriptor, handler );
	}

	internal static string Permission( string command ) => "admin.players." + command;

	/// <summary>Runs one action and, when it worked, announces it with the server's wording for that message key.</summary>
	private CommandResult Act( CommandContext context, string target, string message, Func<IPlayerActions, CommandResult> action )
	{
		var actions = _actions();
		if ( actions is null ) return new CommandResult( CommandStatus.Unsupported, "This game has no player actions configured." );
		var result = action( actions );
		if ( result.Status == CommandStatus.Success ) Announce( context, message, target );
		return result;
	}

	/// <summary>Announces a message key; keys without wording, such as cloak, stay quiet.</summary>
	private void Announce( CommandContext context, string message, string target, string map = null )
	{
		if ( message is not null && _messages.Render( message, ActorName( context ), target is null ? null : _playerName( target ), map: map ) is { } line )
			_announce?.Invoke( line );
	}

	private string ActorName( CommandContext context ) => ActorSteamId( context ) is { } id ? _playerName( id ) : "Console";

	/// <summary>The calling player's Steam ID, or null for the console and host services.</summary>
	private static string ActorSteamId( CommandContext context ) => context.Services is CommandExecutionServices services
		&& services.Authenticate() is { Kind: ActorKind.Player } actor ? actor.SubjectId : null;

	private static CommandResult NeedsPlayer() => new( CommandStatus.Unsupported, "Only a player in the game can do this." );

	private static CommandArgumentDescriptor Target( CommandTargetPolicy policy, string name = "user" ) => new( name,
		"Exact Steam ID or unique online name", CommandArgumentKind.Player, targetPolicy: policy, audit: CommandArgumentAudit.SafeValue );

	private static CommandArgumentDescriptor Amount( string name, string help, long minimum, long maximum, bool optional = false ) => new( name,
		help, CommandArgumentKind.Integer, isOptional: optional, minimum: minimum, maximum: maximum, audit: CommandArgumentAudit.SafeValue );

	// Free text is redacted in the audit log like every other string argument.
	private static CommandArgumentDescriptor Name( string name, string help ) => new( name, help, CommandArgumentKind.String );

	private static string User( CommandArguments arguments, string name = "user" ) => arguments.Get<PlayerReference>( name ).SteamId;

	private static int Number( CommandArguments arguments, string name, int fallback ) =>
		arguments.Contains( name ) ? (int)arguments.Get<long>( name ) : fallback;

	public void Dispose() => _scope.Dispose();
}
