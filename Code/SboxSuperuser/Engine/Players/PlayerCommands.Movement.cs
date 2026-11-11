namespace SboxSuperuser.Engine.Players;

internal sealed partial class PlayerCommands
{
	private void RegisterMovement()
	{
		Register( "bring", "Bring an online player to you", new[] { Target( Punishable ) },
			(context, arguments) => Task.FromResult( WithActor( context, actor => Act( context, User( arguments ), null,
				actions => actions.MoveTo( User( arguments ), actor ) ) ) ) );
		Register( "goto", "Go to an online player", new[] { Target( CommandTargetPolicy.OnlinePlayer ) },
			(context, arguments) => Task.FromResult( WithActor( context, actor => Act( context, actor, null,
				actions => actions.MoveTo( actor, User( arguments ) ) ) ) ) );
		Register( "send", "Send one online player to another", new[] { Target( Punishable ), Target( CommandTargetPolicy.OnlinePlayer, "destination" ) },
			(context, arguments) => Task.FromResult( Act( context, User( arguments ), null,
				actions => actions.MoveTo( User( arguments ), User( arguments, "destination" ) ) ) ) );
		Register( "teleport", "Move an online player to where you are looking", new[] { Target( Punishable ) },
			(context, arguments) => Task.FromResult( WithActor( context, actor => Act( context, User( arguments ), null,
				actions => actions.MoveToAim( User( arguments ), actor ) ) ) ) );
		Register( "return", "Put an online player back where they were before they were last moved", new[] { Target( CommandTargetPolicy.OnlinePlayer ) },
			(context, arguments) => Task.FromResult( Act( context, User( arguments ), null, actions => actions.Return( User( arguments ) ) ) ) );
	}

	private static CommandResult WithActor( CommandContext context, Func<string, CommandResult> action ) =>
		ActorSteamId( context ) is { } actor ? action( actor ) : NeedsPlayer();
}
