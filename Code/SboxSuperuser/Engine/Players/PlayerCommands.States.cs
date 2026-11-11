namespace SboxSuperuser.Engine.Players;

internal sealed partial class PlayerCommands
{
	private const int MaximumHealth = 1000000;

	private void RegisterStates()
	{
		SelfOrProtected( "god", "Make yourself or a lower-ranked online player immune to damage", (actions, target, _) => actions.SetGod( target, true ) );
		SelfOrProtected( "ungod", "Let yourself or a lower-ranked online player take damage again", (actions, target, _) => actions.SetGod( target, false ) );
		SelfOrProtected( "cloak", "Hide your body, or a lower-ranked online player's, from everyone", (actions, target, _) => actions.SetCloaked( target, true ) );
		SelfOrProtected( "uncloak", "Show your body, or a lower-ranked online player's, again", (actions, target, _) => actions.SetCloaked( target, false ) );
		SelfOrProtected( "hp", "Set your health or a lower-ranked online player's", (actions, target, amount) => actions.SetHealth( target, amount ),
			Amount( "amount", "New health", 1, MaximumHealth ) );
		SelfOrProtected( "armor", "Set your armor or a lower-ranked online player's", (actions, target, amount) => actions.SetArmor( target, amount ),
			Amount( "amount", "New armor", 0, MaximumHealth ) );
		Register( "noclip", "Fly through the world, or stop; with a player, toggles theirs",
			new[] { new CommandArgumentDescriptor( "user", "Exact Steam ID or unique online name; yourself when omitted",
				CommandArgumentKind.Player, isOptional: true, targetPolicy: Punishable, audit: CommandArgumentAudit.SafeValue ),
				new CommandArgumentDescriptor( "on", "true or false; toggles when omitted", CommandArgumentKind.Boolean, isOptional: true, audit: CommandArgumentAudit.SafeValue ) },
			(context, arguments) => Task.FromResult( Noclip( context, arguments ) ) );
		Register( "spectate", "Watch an online player through their eyes; run unspectate to stop", new[] { Target( CommandTargetPolicy.OnlinePlayer ) },
			(context, arguments) => Task.FromResult( Spectate( context, User( arguments ) ) ) );
		Register( "unspectate", "Stop spectating and get your own camera back", Array.Empty<CommandArgumentDescriptor>(),
			(context, _) => Task.FromResult( Spectate( context, null ) ) );
	}

	private readonly HashSet<string> _noclipping = new();

	/// <summary>
	/// A command anyone allowed may use on themselves; aiming it at someone else still needs higher protection,
	/// since protection refuses self-targeting outright.
	/// </summary>
	private void SelfOrProtected( string name, string description, Func<IPlayerActions, string, int, CommandResult> action, CommandArgumentDescriptor amount = null )
	{
		var arguments = amount is null ? new[] { Target( CommandTargetPolicy.OnlinePlayer ) } : new[] { Target( CommandTargetPolicy.OnlinePlayer ), amount };
		Register( name, description, arguments, (context, values) =>
		{
			var target = User( values );
			if ( target != ActorSteamId( context ) && !_canTarget( context, target ) )
				return Task.FromResult( new CommandResult( CommandStatus.Denied, "Target protection denied this action." ) );
			return Task.FromResult( Act( context, target, name, actions => action( actions, target, amount is null ? 0 : Number( values, amount.Name, 0 ) ) ) );
		} );
	}

	private CommandResult Noclip( CommandContext context, CommandArguments arguments )
	{
		var target = arguments.Contains( "user" ) ? User( arguments ) : ActorSteamId( context );
		if ( target is null ) return NeedsPlayer();
		var on = arguments.Contains( "on" ) ? arguments.Get<bool>( "on" ) : !_noclipping.Contains( target );
		var result = Act( context, target, null, actions => actions.SetNoclip( target, on ) );
		if ( result.Status != CommandStatus.Success ) return result;
		if ( on ) _noclipping.Add( target ); else _noclipping.Remove( target );
		return new CommandResult( CommandStatus.Success, on ? "Noclip on." : "Noclip off." );
	}

	private CommandResult Spectate( CommandContext context, string target )
	{
		if ( ActorSteamId( context ) is not { } actor ) return NeedsPlayer();
		if ( target == actor ) return new CommandResult( CommandStatus.InvalidArguments, "You can't spectate yourself." );
		return Act( context, actor, null, actions => actions.Spectate( actor, target ) );
	}
}
