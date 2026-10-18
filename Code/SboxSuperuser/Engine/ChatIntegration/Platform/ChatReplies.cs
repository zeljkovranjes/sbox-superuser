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
		if ( Application.IsDedicatedServer || Rpc.Caller is null || !Rpc.Caller.IsHost || requestId == Guid.Empty
			|| !Enum.IsDefined( status ) || message is null || message.Length > 1024 || keys is null || values is null
			|| keys.Length != values.Length || keys.Length > 16 || keys.Any( key => key is null || key.Length > 64 )
			|| values.Any( value => value is null || value.Length > 1024 )
			|| keys.Sum( key => key.Length ) + values.Sum( value => value.Length ) > 8192 )
		{
			return;
		}

		// Local system notifications have no sender and never enter the command transport.
		Chat.AddText( $"[Superuser] {status}: {message}" );
		for ( var index = 0; index < keys.Length; index++ )
		{
			Chat.AddText( $"{keys[ index ]}: {values[ index ]}" );
		}
		Log.Info( $"[Superuser chat {requestId:N}] Private result: {status}." );
	}
}
