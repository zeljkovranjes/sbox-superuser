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
		RegisterModerationAction( "gag", ModerationKind.Gag, CommandTargetPolicy.PunitiveAnyPlayer, timed: true );
		RegisterModerationAction( "ungag", ModerationKind.Ungag, CommandTargetPolicy.AnyPlayer, revoke: true );
		RegisterModerationAction( "gimp", ModerationKind.Gimp, CommandTargetPolicy.PunitiveAnyPlayer, timed: true );
		RegisterModerationAction( "ungimp", ModerationKind.Ungimp, CommandTargetPolicy.AnyPlayer, revoke: true );
		RegisterModerationAction( "note", ModerationKind.Note, CommandTargetPolicy.AnyPlayer );
		Register( "moderation.history", "Read private moderation history for an online player or offline Steam ID", "admin.players.history",
			new[] { Player(), Page() }, (context, arguments) => Task.FromResult( _moderation.History( context, arguments.Get<PlayerReference>( "user" ).SteamId, PageNumber( arguments ) ) ) );
	}

	// Message keys follow the action, so a passed votekick is announced like a kick.
	private static string MessageKey( ModerationKind kind ) => kind switch
	{
		ModerationKind.TextMute => "mute",
		_ => kind.ToString().ToLowerInvariant()
	};

	/// <summary>The server's wording for the action, or none for private records such as notes.</summary>
	private string Announcement( string command, string actor, string target, string reason, CommandDuration duration ) =>
		_messages.Render( command, actor, target, reason, duration is null ? null : Core.Messages.MessageCatalog.Phrase( duration.Value ) );

	private void RegisterModerationAction( string command, ModerationKind kind, CommandTargetPolicy targetPolicy, bool timed = false, bool revoke = false )
	{
		var arguments = new List<CommandArgumentDescriptor>
		{
			new( "user", "Exact Steam ID or unique online name", CommandArgumentKind.Player, targetPolicy: targetPolicy, audit: CommandArgumentAudit.SafeValue )
		};
		if ( timed ) arguments.Add( new( "duration", "Explicit positive duration such as 30m, 2h, 7d, or permanent", CommandArgumentKind.Duration, audit: CommandArgumentAudit.SafeValue ) );
		arguments.Add( new( "reason", "Printable reason, at most 256 characters", CommandArgumentKind.String, isOptional: revoke, isRemainder: true ) );
		var permission = "admin.players." + command;
		var verb = MessageKey( kind );
		Register( command, $"{char.ToUpperInvariant( verb[ 0 ] )}{verb[ 1.. ]} a player with persistent history; " + (revoke ? "omitted reason uses 'Revoked by operator'." : "a reason is required."),
			permission, arguments.ToArray(), (context, values) => ModerateAsync( context, command, permission, kind,
				values.Get<PlayerReference>( "user" ).SteamId, values.Contains( "reason" ) ? values.Get<string>( "reason" ) : "Revoked by operator.",
				timed ? values.Get<CommandDuration>( "duration" ) : null ), mutation: true );
	}

	/// <summary>
	/// Records a moderation action, disconnects for kicks and bans, and announces it. Votes use this too, with their
	/// own command and permission, so a passed votekick is recorded and protected exactly like a kick.
	/// </summary>
	private async Task<CommandResult> ModerateAsync( CommandContext context, string command, string permission, ModerationKind kind,
		string target, string reason, CommandDuration duration )
	{
		var actorName = context.Actor.Kind == ActorKind.InternalService ? "Superuser" : _playerName(context.Actor.SubjectId);
		var targetName = _playerName(target);
		var result = await _moderation.ApplyAsync( context, command, permission, kind, target, reason, duration );
		var newlyPersisted = result.Status == CommandStatus.Success;
		if ( result.Status == CommandStatus.Success && kind == ModerationKind.Warn && _notifyWarning is not null )
		{
			var notified = _notifyWarning( context, target, reason );
			result = new CommandResult( notified.Status, notified.Message, result.Data );
		}
		if ( result.Status == CommandStatus.Success && kind is ModerationKind.Kick or ModerationKind.Ban
			|| result.Status == CommandStatus.NoChange && kind == ModerationKind.Ban )
		{
			if ( _disconnect is null ) return new CommandResult( CommandStatus.Unsupported, "Record persisted; this integration has no disconnect adapter.", result.Data );
			var disconnected = await _disconnect( context, target, permission, kind == ModerationKind.Ban );
			result = new CommandResult( disconnected.Status, disconnected.Message, result.Data );
		}
		if ( newlyPersisted && result.Status == CommandStatus.Success && Announcement( MessageKey( kind ), actorName, targetName, reason, duration ) is { } notice ) _announce?.Invoke( notice );
		return result;
	}
}
