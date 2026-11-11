using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>
	/// Whether this player may pick up, move, tool or delete the object, on the host: its owner and the friends they
	/// shared with may, staff with admin.props.touchall may touch anything, and the map's own objects are staff-only.
	/// Call it from physics guns, tools and delete actions. True when no runtime is running, so games still work.
	/// </summary>
	public static bool CanTouch( Connection actor, GameObject target )
	{
		if ( actor is null || target is null || FindAuthorityRuntime() is not { } runtime ) return true;
		var root = target.Root;
		if ( root.Network.Owner is { } owner ) runtime.Props.Record( root.Id, owner.SteamId.ToString() );
		var steamId = actor.SteamId.ToString();
		var staff = runtime.Policy.Explain( new ActorIdentity( ActorKind.Player, steamId ), Engine.Props.PropCommands.TouchAllPermission ).IsAllowed;
		return runtime.Props.CanTouch( steamId, root.Id, staff );
	}

	/// <summary>The Steam ID of the player who spawned the object, or null for the map's own objects.</summary>
	public static string PropOwner( GameObject target ) => target is null ? null : FindAuthorityRuntime()?.Props.OwnerOf( target.Root.Id );

	internal static Core.Props.PropOwnership Props => FindAuthorityRuntime()?.Props;

	internal static Engine.Props.IPropWorld PropWorld => FindAuthorityRuntime()?.PropWorld;
}
