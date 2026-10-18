using Sandbox;

namespace SboxSuperuser.Engine.Console;

internal static class ConsoleReplies
{
	internal static void Send( Connection recipient, bool dedicatedLocal, Guid requestId, CommandResult result )
	{
		if ( dedicatedLocal && Application.IsDedicatedServer )
		{
			Print( requestId, result.Status, result.Message, result.Data.Keys.ToArray(), result.Data.Values.ToArray() );
			return;
		}

		if ( !Networking.IsHost || recipient is null || !recipient.IsActive )
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
			|| message is null || message.Length > 1024 || keys is null || values is null || keys.Length != values.Length
			|| keys.Length > 16 || keys.Any( key => key is null || key.Length > 64 ) || values.Any( value => value is null || value.Length > 1024 )
			|| keys.Sum( key => key.Length ) + values.Sum( value => value.Length ) > 8192 )
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
