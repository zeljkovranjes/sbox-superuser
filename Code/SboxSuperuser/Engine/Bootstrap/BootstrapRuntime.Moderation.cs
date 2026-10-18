using Sandbox;
using SboxSuperuser.Core.Moderation;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed partial class BootstrapRuntime
{
	private bool ModerationAvailable() => ThreadSafe.IsMainThread && IsUsable;
	private bool CanModerateTarget( ActorIdentity actor, string target ) => Policy.CheckTarget( actor, target ).IsAllowed;
	private static void ReportModerationFailure( Exception error ) => Log.Warning( $"[Superuser] Moderation storage degraded; type={error.GetType().Name}; admission fails closed." );

	private CommandResult NotifyModerationWarning( CommandContext context, string target, string reason )
	{
		if ( !Dispatcher.CanUse( context, "admin.players.warn" ) || !CanModerateTarget( context.Actor, target ) )
			return new CommandResult( CommandStatus.Unknown, "Warning persisted; authority changed before notification." );
		foreach ( var connection in Connection.All.Where( connection => connection.IsActive && connection.SteamId.ToString() == target ) )
			connection.SendLog( LogLevel.Warn, "Server warning: " + reason );
		return new CommandResult( CommandStatus.Success, "Warning persisted; private console notification sent to any active target connection." );
	}

	internal bool AcceptConnection( Connection connection, ref string reason )
	{
		if ( connection is null || connection.SteamId == 0 )
		{
			reason = "A valid player identity is required.";
			return false;
		}
		var result = Moderation.CheckRestriction( connection.SteamId.ToString(), ModerationKind.Ban );
		if ( result.Status == CommandStatus.Success ) return true;
		reason = result.Message;
		return false;
	}

	private async Task<CommandResult> DisconnectModeratedAsync( CommandContext context, string target, string permission, bool ban )
	{
		if ( !Dispatcher.CanUse( context, permission ) || !CanModerateTarget( context.Actor, target ) )
			return new CommandResult( CommandStatus.Unknown, "Record persisted; authority changed before disconnect, so no disconnect was attempted." );
		if ( ban )
		{
			var restriction = Moderation.CheckRestriction( target, ModerationKind.Ban );
			if ( restriction.Status != CommandStatus.Denied )
				return new CommandResult( restriction.Status == CommandStatus.Success ? CommandStatus.NoChange : CommandStatus.Unknown,
					"Ban record persisted but is no longer confirmed active; no disconnect was attempted." );
		}
		var connections = Connection.All.Where( connection => connection.IsActive && !connection.IsHost && connection.SteamId.ToString() == target ).ToArray();
		if ( connections.Length == 0 ) return new CommandResult( ban ? CommandStatus.Success : CommandStatus.NoChange,
			ban ? "Ban persisted; target is offline." : "Kick intent persisted; target left before disconnect." );
		try
		{
			foreach ( var connection in connections ) connection.Kick( ban ? "Connection denied by server moderation." : "Removed by server moderation." );
			for ( var attempt = 0; attempt < 40; attempt++ )
			{
				if ( !connections.Any( connection => connection.IsActive && Connection.All.Any( current => ReferenceEquals( current, connection ) ) ) )
					return new CommandResult( CommandStatus.Success, ban ? "Ban persisted and disconnect confirmed." : "Disconnect confirmed." );
				await Task.Delay( 25, context.Cancellation );
			}
		}
		catch ( Exception error ) when ( error is not OperationCanceledException )
		{
			Log.Warning( $"[Superuser] Moderation disconnect failed; type={error.GetType().Name}." );
		}
		return new CommandResult( CommandStatus.Unknown, "Record persisted; disconnect was requested but not confirmed." );
	}
}
