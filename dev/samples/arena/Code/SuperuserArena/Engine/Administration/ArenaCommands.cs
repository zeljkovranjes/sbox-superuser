using SboxSuperuser;

namespace SuperuserArena.Engine.Administration;

internal static class ArenaCommands
{
	internal static CommandRegistrationScope Register( IPlayerRecovery recovery )
	{
		var registration = Superuser.BeginCommandRegistration( "arena.game" );
		try
		{
			registration.RegisterPermission( new PermissionDescriptor( "arena.heal", "Restore an arena pawn" ) );
			if ( recovery is not null ) registration.RegisterService<IPlayerRecovery>( recovery );
			registration.RegisterCommand( new CommandDescriptor( "arena.heal", "Restore a game's arena pawn", "arena.heal", "arena.game",
				new[] { new CommandArgumentDescriptor( "player", "Stable player identifier", CommandArgumentKind.Player, targetPolicy: CommandTargetPolicy.AnyPlayer ) } ),
				( context, arguments ) => Task.FromResult( context.Services.GetService<IPlayerRecovery>()?.Restore( arguments.Get<PlayerReference>( "player" ) )
					?? new CommandResult( CommandStatus.Unsupported, "This game has not installed the arena recovery capability." ) ) );
			registration.Commit();
			return registration;
		}
		catch
		{
			registration.Dispose();
			throw;
		}
	}
}
