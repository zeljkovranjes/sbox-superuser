using SboxSuperuser;

namespace SuperuserColony.Engine.Administration;

internal static class ColonyCommands
{
	internal static CommandRegistrationScope Register( IColonySupplies supplies )
	{
		var registration = Superuser.BeginCommandRegistration( "colony.game" );
		try
		{
			registration.RegisterPermission( new PermissionDescriptor( "colony.supply", "Replenish a colony member's supplies" ) );
			if ( supplies is not null ) registration.RegisterService<IColonySupplies>( supplies );
			registration.RegisterCommand( new CommandDescriptor( "colony.supply", "Replenish a game's roster entry", "colony.supply", "colony.game",
				new[] { new CommandArgumentDescriptor( "player", "Stable player identifier", CommandArgumentKind.Player, targetPolicy: CommandTargetPolicy.AnyPlayer ) } ),
				( context, arguments ) => Task.FromResult( context.Services.GetService<IColonySupplies>()?.Replenish( arguments.Get<PlayerReference>( "player" ) )
					?? new CommandResult( CommandStatus.Unsupported, "This game has not installed the colony supply capability." ) ) );
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
