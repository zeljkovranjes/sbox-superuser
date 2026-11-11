using Sandbox;
namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Rechecks host-issued execution authority after asynchronous work; forged contexts fail closed.</summary>
	public static bool CanExecute(CommandContext context,string permission) => context is not null && FindAuthorityRuntime()?.Dispatcher.CanUse(context,permission)==true;

	/// <summary>Rechecks permission and target protection after asynchronous module work.</summary>
	public static bool CanTarget(CommandContext context,string permission,string target) => CanExecute(context,permission) && FindAuthorityRuntime().Dispatcher.CanTarget(context,target);

	/// <summary>Returns an online display name or the exact offline identity.</summary>
	public static string PlayerName(string steamId)
	{
		foreach(var player in Connection.All) if(player.SteamId.ToString()==steamId) return player.DisplayName;
		return steamId;
	}

	/// <summary>Broadcasts plain server text; callable only by trusted host code, never a client request.</summary>
	public static void Announce(string message)
	{
		if(!Networking.IsHost || FindAuthorityRuntime() is null) throw new InvalidOperationException("An authoritative Superuser runtime is required.");
		if(string.IsNullOrWhiteSpace(message) || message.Length>1500 || message.Any(char.IsControl)) throw new ArgumentException("Announcements must be a single printable line of at most 1500 characters.");
		try { ReceiveAnnouncement(AnnouncementTag, message); }
		catch(Exception error) { Log.Error($"[Superuser] Moderation succeeded but announcement delivery failed: {error.Message}"); }
	}

	private static readonly Core.Messages.MessageCatalog BuiltInMessages = new();

	/// <summary>The server's message wording, or the built-in wording when no runtime is authoritative.</summary>
	internal static Core.Messages.MessageCatalog MessageCatalog => FindAuthorityRuntime()?.Messages?.Catalog ?? BuiltInMessages;

	/// <summary>The server's announcement tag, SUPERUSER unless it was changed with admin messages set tag.</summary>
	internal static string AnnouncementTag => MessageCatalog.Tag;

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private static void ReceiveAnnouncement(string tag, string message)
	{
		if(Rpc.Caller is not {IsHost:true} || message is null || message.Length>1500 || tag is null || tag.Length>Core.Messages.MessageCatalog.MaximumTagLength) return;
		var notification=new AdminAnnouncementEvent(message);
		Game.ActiveScene?.RunEvent<IAdminAnnouncementEvent>(listener=>listener.OnAdminAnnouncement(notification));
		if(notification.SuppressDefaultDisplay) return;
		// Superuser's own chat box draws the tag so it stands out; the built-in chat only has plain text.
		if(Game.ActiveScene?.GetSystem<Systems.ChatBoxSystem>()?.ShowAnnouncement(tag,message)!=true) Sandbox.Platform.Chat.AddText(tag+": "+message);
	}
}
