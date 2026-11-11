namespace SboxSuperuser.Engine.Players;

internal sealed partial class PlayerCommands
{
	private const CommandTargetPolicy Punishable = CommandTargetPolicy.PunitiveOnlinePlayer;

	private void RegisterPunishments()
	{
		Single( "slay", "Kill an online player", "slay", (actions, target, _) => actions.Kill( target, false ) );
		Single( "sslay", "Kill an online player silently, without an announcement", null, (actions, target, _) => actions.Kill( target, true ) );
		Register( "slap", "Knock an online player around, with optional damage",
			new[] { Target( Punishable ), Amount( "damage", "Damage per slap, 0 by default", 0, MaximumDamage, optional: true ) },
			(context, arguments) => Task.FromResult( Act( context, User( arguments ), "slap",
				actions => actions.Slap( User( arguments ), Number( arguments, "damage", 0 ) ) ) ) );
		Register( "whip", "Slap an online player repeatedly, half a second apart",
			new[] { Target( Punishable ), Amount( "times", $"Number of slaps, 10 by default, at most {MaximumWhips}", 1, MaximumWhips, optional: true ),
				Amount( "damage", "Damage per slap, 0 by default", 0, MaximumDamage, optional: true ) },
			(context, arguments) => WhipAsync( context, User( arguments ), Number( arguments, "times", 10 ), Number( arguments, "damage", 0 ), false ) );
		Register( "maul", "Beat an online player with heavy blows until they die", new[] { Target( Punishable ) },
			(context, arguments) => WhipAsync( context, User( arguments ), MaulBlows, MaulDamage, true ) );
		Toggle( "freeze", "unfreeze", "Stop or restore an online player's movement", (actions, target, on) => actions.SetFrozen( target, on ) );
		Toggle( "blind", "unblind", "Black out or restore an online player's screen", (actions, target, on) => actions.SetBlind( target, on ) );
		Toggle( "ignite", "unignite", "Set an online player on fire or put them out", (actions, target, on) => actions.SetIgnited( target, on ) );
		Register( "unigniteall", "Put out every burning player", Array.Empty<CommandArgumentDescriptor>(),
			(context, _) => Task.FromResult( Everyone( actions => target => actions.SetIgnited( target, false ), "Put out every player." ) ) );
		Toggle( "jail", "unjail", "Cage an online player where they stand, or free them", (actions, target, on) => actions.SetJailed( target, on ) );
		Register( "jailtp", "Move an online player to where you are looking and cage them there", new[] { Target( Punishable ) },
			(context, arguments) => Task.FromResult( JailAtAim( context, User( arguments ) ) ) );
		Single( "strip", "Take away an online player's weapons and items", "strip", (actions, target, _) => actions.Strip( target ) );
		Toggle( "ragdoll", "unragdoll", "Turn an online player into a ragdoll, or stand them back up", (actions, target, on) => actions.SetRagdoll( target, on ) );
	}

	private async Task<CommandResult> WhipAsync( CommandContext context, string target, int times, float damage, bool kill )
	{
		var message = kill ? "maul" : "whip";
		for ( var blow = 0; blow < times; blow++ )
		{
			if ( blow > 0 ) await _wait( WhipInterval );
			var result = Act( context, target, null, actions => actions.Slap( target, damage ) );
			if ( result.Status != CommandStatus.Success ) return blow == 0 ? result : new CommandResult( CommandStatus.Success, $"Stopped after {blow} blows: {result.Message}" );
		}
		if ( kill ) return Act( context, target, message, actions => actions.Kill( target, false ) );
		Announce( context, message, target );
		return new CommandResult( CommandStatus.Success, $"Whipped {times} times." );
	}

	private CommandResult JailAtAim( CommandContext context, string target )
	{
		if ( ActorSteamId( context ) is not { } actor ) return NeedsPlayer();
		var moved = Act( context, target, null, actions => actions.MoveToAim( target, actor ) );
		return moved.Status != CommandStatus.Success ? moved : Act( context, target, "jail", actions => actions.SetJailed( target, true ) );
	}

	/// <summary>Applies an action to every online player and reports how many it worked for.</summary>
	private CommandResult Everyone( Func<IPlayerActions, Func<string, CommandResult>> action, string message )
	{
		if ( _actions() is not { } actions ) return new CommandResult( CommandStatus.Unsupported, "This game has no player actions configured." );
		var apply = action( actions );
		var worked = _online().Count( steamId => apply( steamId ).Status == CommandStatus.Success );
		return new CommandResult( CommandStatus.Success, $"{message} ({worked} affected)" );
	}

	/// <summary>A command with one punishable target.</summary>
	private void Single( string name, string description, string message, Func<IPlayerActions, string, bool, CommandResult> action ) =>
		Register( name, description, new[] { Target( Punishable ) },
			(context, arguments) => Task.FromResult( Act( context, User( arguments ), message, actions => action( actions, User( arguments ), true ) ) ) );

	/// <summary>An on/off pair announced as their own message keys; turning it on is a punishment, off only needs an online player.</summary>
	private void Toggle( string on, string off, string description, Func<IPlayerActions, string, bool, CommandResult> action )
	{
		Register( on, description, new[] { Target( Punishable ) },
			(context, arguments) => Task.FromResult( Act( context, User( arguments ), on, actions => action( actions, User( arguments ), true ) ) ) );
		Register( off, description, new[] { Target( CommandTargetPolicy.OnlinePlayer ) },
			(context, arguments) => Task.FromResult( Act( context, User( arguments ), off, actions => action( actions, User( arguments ), false ) ) ) );
	}
}
