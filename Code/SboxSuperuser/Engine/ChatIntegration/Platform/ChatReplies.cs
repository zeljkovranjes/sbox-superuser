using Sandbox;
using Sandbox.Platform;

namespace SboxSuperuser.Engine.ChatIntegration.Platform;

internal static class ChatReplies
{
	internal static void Send( Connection recipient, Guid requestId, CommandResult result )
	{
		if ( !Networking.IsHost || recipient is null || !recipient.IsActive
			|| !Connection.All.Any( current => ReferenceEquals( current, recipient ) ) )
		{
			return;
		}
		using ( Rpc.FilterInclude( recipient ) )
		{
			Receive( requestId, result.Status, result.Message, result.Data.Keys.ToArray(), result.Data.Values.ToArray() );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private static void Receive( Guid requestId, CommandStatus status, string message, string[] keys, string[] values )
	{
		if ( Application.IsDedicatedServer || Rpc.Caller is null || !Rpc.Caller.IsHost
			|| !Commands.PrivateReplyValidation.IsValid( requestId, status, message, keys, values ) )
		{
			return;
		}

		// Local system notifications have no sender and never enter the command transport.
		var fields = keys.Select( (key, index) => new KeyValuePair<string, string>( key, values[ index ] ) ).ToDictionary( pair => pair.Key, pair => pair.Value );
		var result = new CommandResult( status, message, fields );
		foreach ( var line in ChatResultText.Lines( result ) )
		{
			Chat.AddText( line );
		}
		Log.Info( $"[Superuser chat {requestId:N}] Private result: {status}." );
	}
}
