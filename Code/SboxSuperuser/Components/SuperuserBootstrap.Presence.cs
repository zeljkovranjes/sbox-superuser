using Sandbox;

namespace SboxSuperuser.Components;

/// <summary>Replaces the engine's join and leave lines with the server's own wording, without the emoji.</summary>
public sealed partial class SuperuserBootstrap
{
	private const string EngineJoinPrefix = "👋 ";

	void Component.INetworkListener.OnActive( Connection connection )
	{
		if ( Networking.IsHost )
		{
			_session?.GetRuntime()?.ApplyEngineClaims( connection );
			_session?.GetRuntime()?.RecordConnection( connection );
			Scene.GetSystem<Systems.PropTrackingSystem>()?.PlayerReturned( connection.SteamId.ToString() );
		}
		AnnouncePresence( connection, "join" );
	}

	void Component.INetworkListener.OnDisconnected( Connection connection )
	{
		if ( Networking.IsHost )
		{
			_session?.GetRuntime()?.ForgetEngineClaims( connection );
			Scene.GetSystem<Systems.PropTrackingSystem>()?.PlayerLeft( connection.SteamId.ToString() );
		}
		AnnouncePresence( connection, "leave" );
	}

	private static void AnnouncePresence( Connection connection, string message )
	{
		if ( !Networking.IsHost || connection is null ) return;
		var catalog = Superuser.MessageCatalog;
		if ( catalog.Render( message, target: connection.DisplayName ) is { } line ) Superuser.BroadcastChatLine( line );
	}

	/// <summary>The engine's own "👋 name has joined the game" and "👋 name left the game" notices.</summary>
	private static bool IsEnginePresence( ChatMessageEvent message ) => message.Sender is null && message.Message is { } text
		&& text.StartsWith( EngineJoinPrefix, StringComparison.Ordinal )
		&& (text.EndsWith( " has joined the game", StringComparison.Ordinal ) || text.EndsWith( " left the game", StringComparison.Ordinal ));
}
