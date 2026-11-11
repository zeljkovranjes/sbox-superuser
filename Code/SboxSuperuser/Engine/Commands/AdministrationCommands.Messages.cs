using SboxSuperuser.Core.Moderation;
using SboxSuperuser.Engine.Messaging;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	/// <summary>Staff who receive asay messages.</summary>
	internal const string StaffChatReadPermission = "admin.chat.asay.read";
	private const int MaximumMessageLength = 256;

	private void RegisterMessages( IPlayerMessenger messenger )
	{
		Register( "psay", "Send one online player a private message", "admin.chat.psay",
			new[] { OnlinePlayer(), Message() }, (context, arguments) => Task.FromResult( PrivateSay( context, messenger,
				arguments.Get<PlayerReference>( "user" ).SteamId, arguments.Get<string>( "message" ) ) ), mutation: true );
		Register( "asay", "Send a message to online staff; anyone may write, staff with admin.chat.asay.read receive", "admin.chat.asay",
			new[] { Message() }, (context, arguments) => Task.FromResult( StaffSay( context, messenger, arguments.Get<string>( "message" ) ) ), mutation: true );
		Register( "csay", "Show a message in the middle of every player's screen", "admin.chat.csay",
			new[] { Message() }, (context, arguments) => Task.FromResult( Broadcast( context, arguments.Get<string>( "message" ), messenger.SendCenter ) ), mutation: true );
		Register( "tsay", "Post a message in everyone's chat without a name", "admin.chat.tsay",
			new[] { Message() }, (context, arguments) => Task.FromResult( Broadcast( context, arguments.Get<string>( "message" ), messenger.SendChat ) ), mutation: true );
		Register( "thetime", "Show the server's local time and UTC", "admin.chat.thetime",
			Array.Empty<CommandArgumentDescriptor>(), (_, _) => Task.FromResult( TheTime( DateTimeOffset.Now ) ) );
	}

	private CommandResult PrivateSay( CommandContext context, IPlayerMessenger messenger, string target, string message )
	{
		if ( InvalidMessage( message ) is { } invalid ) return invalid;
		if ( IsMuted( context ) ) return Muted();
		var sender = SenderName( context );
		if ( !messenger.SendPrivate( target, _messages.Render( "psay", sender, message: message ) ) )
			return new CommandResult( CommandStatus.NotFound, "That player is no longer connected." );
		return new CommandResult( CommandStatus.Success, $"Sent to {_playerName( target )}: {message}" );
	}

	private CommandResult StaffSay( CommandContext context, IPlayerMessenger messenger, string message )
	{
		if ( InvalidMessage( message ) is { } invalid ) return invalid;
		if ( IsMuted( context ) ) return Muted();
		var line = _messages.Render( "asay", SenderName( context ), message: message );
		var sender = Authenticate( context )?.SubjectId;
		var delivered = 0;
		foreach ( var steamId in messenger.Online )
		{
			// The sender sees their own line even without read access, so they know it went out.
			if ( steamId != sender && !_policy.Explain( new ActorIdentity( ActorKind.Player, steamId ), StaffChatReadPermission ).IsAllowed ) continue;
			if ( messenger.SendPrivate( steamId, line ) && steamId != sender ) delivered++;
		}
		return new CommandResult( CommandStatus.Success, delivered == 0 ? "No staff are online to read it." : $"Sent to {delivered} staff." );
	}

	private static CommandResult Broadcast( CommandContext context, string message, Action<string> send )
	{
		if ( InvalidMessage( message ) is { } invalid ) return invalid;
		send( message );
		return new CommandResult( CommandStatus.Success, "Sent to everyone." );
	}

	internal static CommandResult TheTime( DateTimeOffset now ) => new( CommandStatus.Success,
		$"Server time: {now:yyyy-MM-dd HH:mm:ss} (UTC{now:zzz}); UTC {now.UtcDateTime:yyyy-MM-dd HH:mm:ss}." );

	// Mutes cover text chat, and private and staff messages are text chat by another route.
	private bool IsMuted( CommandContext context ) => Authenticate( context ) is { Kind: ActorKind.Player } actor
		&& _moderation?.IsRestricted( actor.SubjectId, ModerationKind.TextMute ) == true;

	private static CommandResult InvalidMessage( string message ) =>
		string.IsNullOrWhiteSpace( message ) || message.Length > MaximumMessageLength || message.Any( char.IsControl )
			? new CommandResult( CommandStatus.InvalidArguments, $"Messages must be printable text of 1–{MaximumMessageLength} characters." )
			: null;

	private static CommandResult Muted() => new( CommandStatus.Denied, "You are muted." );

	private string SenderName( CommandContext context ) => Authenticate( context ) is { Kind: ActorKind.Player } actor
		? _playerName( actor.SubjectId ) : "Console";

	private static CommandArgumentDescriptor OnlinePlayer() => new( "user", "Exact Steam ID or unique online name", CommandArgumentKind.Player,
		targetPolicy: CommandTargetPolicy.OnlinePlayer, audit: CommandArgumentAudit.SafeValue );

	private static CommandArgumentDescriptor Message() => new( "message", $"Printable text, at most {MaximumMessageLength} characters",
		CommandArgumentKind.String, isRemainder: true );
}
