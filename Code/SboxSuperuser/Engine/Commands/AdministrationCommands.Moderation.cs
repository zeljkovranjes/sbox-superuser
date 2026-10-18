using SboxSuperuser.Core.Moderation;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	private void RegisterModeration()
	{
		RegisterModerationAction( "kick", ModerationKind.Kick, CommandTargetPolicy.PunitiveOnlinePlayer );
		RegisterModerationAction( "ban", ModerationKind.Ban, CommandTargetPolicy.PunitiveAnyPlayer, timed: true );
		RegisterModerationAction( "warn", ModerationKind.Warn, CommandTargetPolicy.PunitiveAnyPlayer );
		RegisterModerationAction( "mute", ModerationKind.TextMute, CommandTargetPolicy.PunitiveAnyPlayer, timed: true );
		RegisterModerationAction( "unban", ModerationKind.Unban, CommandTargetPolicy.AnyPlayer, revoke: true );
		RegisterModerationAction( "unmute", ModerationKind.Unmute, CommandTargetPolicy.AnyPlayer, revoke: true );
		Register( "moderation.history", "Read private moderation history for an online player or offline Steam ID", "admin.players.history",
			new[] { Player(), Page() }, (context, arguments) => Task.FromResult( _moderation.History( context, arguments.Get<PlayerReference>( "user" ).SteamId, PageNumber( arguments ) ) ) );
	}

	private void RegisterModerationAction( string command, ModerationKind kind, CommandTargetPolicy targetPolicy, bool timed = false, bool revoke = false )
	{
		var arguments = new List<CommandArgumentDescriptor>
		{
			new( "user", "Exact Steam ID or unique online name", CommandArgumentKind.Player, targetPolicy: targetPolicy, audit: CommandArgumentAudit.SafeValue )
		};
		if ( timed ) arguments.Add( new( "duration", "Explicit positive duration such as 30m, 2h, 7d, or permanent", CommandArgumentKind.Duration, audit: CommandArgumentAudit.SafeValue ) );
		arguments.Add( new( "reason", "Printable reason, at most 256 characters", CommandArgumentKind.String, isOptional: revoke, isRemainder: true ) );
		var permission = "admin.players." + command;
		Register( command, $"{kind} a player with persistent history; " + (revoke ? "omitted reason uses 'Revoked by operator'." : "a reason is required."),
			permission, arguments.ToArray(), async (context, values) =>
			{
				var target = values.Get<PlayerReference>( "user" ).SteamId;
				var reason = values.Contains( "reason" ) ? values.Get<string>( "reason" ) : "Revoked by operator.";
				var duration = timed ? values.Get<CommandDuration>( "duration" ) : null;
				var result = await _moderation.ApplyAsync( context, command, permission, kind, target, reason, duration );
				if ( result.Status != CommandStatus.Success || kind is not (ModerationKind.Kick or ModerationKind.Ban) ) return result;
				if ( _disconnect is null ) return new CommandResult( CommandStatus.Unsupported, "Record persisted; this integration has no disconnect adapter.", result.Data );
				var disconnected = await _disconnect( context, target, permission, kind == ModerationKind.Ban );
				return new CommandResult( disconnected.Status, disconnected.Message, result.Data );
			}, mutation: true );
	}
}
