using Sandbox;

namespace SboxSuperuser.Engine.Messaging;

/// <summary>Live delivery as host-only broadcasts; private lines are filtered to their one recipient.</summary>
internal sealed class EngineMessenger : IPlayerMessenger
{
	public IReadOnlyList<string> Online => Connection.All.Where( connection => connection.IsActive )
		.Select( connection => connection.SteamId.ToString() ).ToArray();

	public bool SendPrivate( string steamId, string text )
	{
		var recipient = Connection.All.FirstOrDefault( connection => connection.IsActive && connection.SteamId.ToString() == steamId );
		if ( recipient is null ) return false;
		// A plain chat line for one recipient, not a command reply with a status prefix.
		using ( Rpc.FilterInclude( recipient ) ) Superuser.BroadcastChatLine( text );
		return true;
	}

	public void SendChat( string text ) => Superuser.BroadcastChatLine( text );

	public void SendCenter( string text ) => Superuser.BroadcastCenterText( text );
}
