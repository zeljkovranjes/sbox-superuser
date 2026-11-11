using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>
	/// Whether the server allows this map, addon or entity; false when an admin banned it with admin content ban.
	/// Call it on the host before spawning or mounting something a player asked for. Superuser removes banned
	/// entities players spawn anyway, but addons can only be refused here. True when no runtime is running.
	/// </summary>
	public static bool IsContentAllowed( ContentKind kind, string id ) =>
		FindAuthorityRuntime()?.Content?.IsAllowed( kind.ToString().ToLowerInvariant(), id ) ?? true;

	internal static Core.Content.ContentBanList ContentBans => FindAuthorityRuntime()?.Content?.Bans;
}
