using Sandbox;

namespace SboxSuperuser.Systems;

/// <summary>
/// On the host, records who spawned each object so prop protection still knows after they leave, and removes a
/// player's props a while after they disconnect unless they come back. Player bodies are never counted as props.
/// </summary>
internal sealed class PropTrackingSystem : GameObjectSystem<PropTrackingSystem>
{
	private const float Interval = 0.5f;

	private readonly Dictionary<string, DateTimeOffset> _left = new( StringComparer.Ordinal );
	private RealTimeSince _sinceSweep;

	public PropTrackingSystem( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 49, Sweep, nameof( PropTrackingSystem ) );

	/// <summary>Starts the countdown to removing this player's props; a null delay keeps them.</summary>
	internal void PlayerLeft( string steamId, TimeSpan? delay )
	{
		if ( delay is { } wait ) _left[ steamId ] = DateTimeOffset.UtcNow + wait;
	}

	internal void PlayerReturned( string steamId ) => _left.Remove( steamId );

	private void Sweep()
	{
		if ( _sinceSweep < Interval || !Networking.IsHost || Superuser.Props is not { } props ) return;
		_sinceSweep = 0;
		foreach ( var root in Scene.Children )
		{
			if ( root.Network.Owner is not { } owner || root.GetComponentInChildren<PlayerController>( true ) is not null ) continue;
			props.Record( root.Id, owner.SteamId.ToString() );
		}
		foreach ( var id in props.All )
			if ( Scene.Directory.FindByGuid( id ) is null ) props.Forget( id );
		if ( Superuser.PropWorld is { } world ) CleanUpAfterLeavers( props, world );
	}

	private void CleanUpAfterLeavers( Core.Props.PropOwnership props, Engine.Props.IPropWorld world )
	{
		if ( _left.Count == 0 ) return;
		var now = DateTimeOffset.UtcNow;
		foreach ( var (steamId, due) in _left.ToArray() )
		{
			if ( due > now ) continue;
			_left.Remove( steamId );
			var objects = props.ObjectsOf( steamId );
			var removed = world.Remove( objects );
			foreach ( var id in objects ) props.Forget( id );
			if ( removed > 0 ) Log.Info( $"[Superuser] Removed {removed} props left behind by {steamId}." );
		}
	}
}
