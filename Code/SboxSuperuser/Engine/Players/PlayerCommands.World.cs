namespace SboxSuperuser.Engine.Players;

internal sealed partial class PlayerCommands
{
	private const int MaximumAssetPathLength = 256;

	private void RegisterWorld()
	{
		Register( "playsound", "Play a sound asset for everyone, such as sounds/kenney/ui/ui.navigate.back.sound",
			new[] { Name( "sound", "Sound asset path" ) },
			(context, arguments) => Task.FromResult( World( context, arguments.Get<string>( "sound" ), actions => actions.PlaySound ) ) );
		Register( "map", "Change the map for everyone; they stay connected",
			new[] { Name( "map", "Map package ident such as facepunch.flatgrass, or a .vmap path" ) },
			(context, arguments) => Task.FromResult( _mapAllowed?.Invoke( arguments.Get<string>( "map" ) ) == false
				? new CommandResult( CommandStatus.Denied, $"{arguments.Get<string>( "map" )} is banned on this server." )
				: World( context, arguments.Get<string>( "map" ), actions => actions.ChangeMap, "map" ) ) );
	}

	private CommandResult World( CommandContext context, string asset, Func<IPlayerActions, Func<string, CommandResult>> action, string message = null )
	{
		if ( string.IsNullOrWhiteSpace( asset ) || asset.Length > MaximumAssetPathLength || asset.Any( char.IsControl ) || asset.Contains( ".." ) )
			return new CommandResult( CommandStatus.InvalidArguments, $"Asset names are printable paths of at most {MaximumAssetPathLength} characters." );
		if ( _actions() is not { } actions ) return new CommandResult( CommandStatus.Unsupported, "This game has no player actions configured." );
		var result = action( actions )( asset );
		if ( result.Status == CommandStatus.Success ) Announce( context, message, null, asset );
		return result;
	}
}
