using Sandbox;

namespace SboxSuperuser.Systems;

/// <summary>
/// On the host, removes banned entities players spawn, shortly after they appear: anything owned by a player whose
/// prefab, model or component type matches an entity ban. The scene's and map's own objects belong to no player and
/// are never touched. It looks twice a second, not every frame, and checks each object once.
/// </summary>
internal sealed class ContentSweepSystem : GameObjectSystem<ContentSweepSystem>
{
	private const float Interval = 0.5f;
	private readonly HashSet<Guid> _checked = new();
	private RealTimeSince _sinceSweep;

	public ContentSweepSystem( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 50, Sweep, nameof( ContentSweepSystem ) );

	private void Sweep()
	{
		if ( _sinceSweep < Interval || !Networking.IsHost ) return;
		_sinceSweep = 0;
		if ( Superuser.ContentBans is not { } bans || bans.Bans.Count == 0 ) return;
		foreach ( var root in Scene.Children )
		{
			if ( !_checked.Add( root.Id ) || root.Network.Owner is not { } owner ) continue;
			if ( BannedPart( root, bans ) is not { } ban ) continue;
			Log.Info( $"[Superuser] Removed banned {ban.Pattern} spawned by {owner.DisplayName}." );
			Superuser.Notify( owner, $"{ban.Pattern} is banned on this server{(ban.Reason.Length > 0 ? ": " + ban.Reason : ".")}" );
			root.Destroy();
		}
		// Forget objects that are gone so the set doesn't grow forever.
		if ( _checked.Count > 4096 ) _checked.RemoveWhere( id => Scene.Directory.FindByGuid( id ) is null );
	}

	private static Core.Content.ContentBan BannedPart( GameObject root, Core.Content.ContentBanList bans )
	{
		const string kind = Core.Content.ContentBanList.Entity;
		if ( bans.Find( kind, root.PrefabInstanceSource ) is { } prefab ) return prefab;
		foreach ( var component in root.GetComponentsInChildren<Component>( true ) )
		{
			var type = bans.Find( kind, component.GetType().Name ) ?? bans.Find( kind, component.GetType().FullName );
			if ( type is not null ) return type;
			if ( component is ModelRenderer { Model: { } model } && bans.Find( kind, model.ResourcePath ) is { } modelBan ) return modelBan;
		}
		return null;
	}
}
