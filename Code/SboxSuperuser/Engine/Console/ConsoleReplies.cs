using Sandbox;

namespace SboxSuperuser.Engine.Console;

internal static class ConsoleReplies
{
	internal static void Send( Connection recipient, bool dedicatedLocal, Guid requestId, CommandResult result )
	{
		if ( Application.IsDedicatedServer && (dedicatedLocal || recipient is not null
			&& ReferenceEquals( recipient, Connection.Host ) && recipient.SteamId == 0) )
		{
			Print( requestId, result.Status, result.Message, result.Data.Keys.ToArray(), result.Data.Values.ToArray() );
			return;
		}

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

		Print( requestId, status, message, keys, values );
	}

	private static void Print( Guid requestId, CommandStatus status, string message, string[] keys, string[] values )
	{
		Log.Info( $"[Superuser {requestId:N}] {status}: {message}" );
		for ( var index = 0; index < keys.Length; index++ )
		{
			Log.Info( $"[Superuser {requestId:N}] {keys[ index ]}: {values[ index ]}" );
		}
	}
}
