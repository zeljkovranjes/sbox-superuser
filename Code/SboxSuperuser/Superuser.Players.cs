using Sandbox;
using SboxSuperuser.Engine.Bodies;

namespace SboxSuperuser;

public static partial class Superuser
{
	private static IPlayerActions _playerActions;

	/// <summary>
	/// Chooses how player commands such as slay, freeze and bring act on bodies. Without a call, Superuser uses
	/// PlayerControllerActions. Pass null to go back to it. Call from trusted host code, typically on startup.
	/// </summary>
	public static void ConfigurePlayerActions( IPlayerActions actions ) => _playerActions = actions;

	internal static IPlayerActions PlayerActions => _playerActions ??= new PlayerControllerActions();

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void ApplyBodyEffect( Guid controllerId, BodyEffect effect, Vector3 vector )
	{
		if ( Rpc.Caller is not { IsHost: true } || FindController( controllerId ) is not { } controller ) return;
		BodyEffects.Apply( controller, effect, vector );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void SetCloak( Guid controllerId, bool cloaked )
	{
		if ( Rpc.Caller is not { IsHost: true } || FindController( controllerId ) is not { } controller ) return;
		BodyEffects.SetCloaked( controller, cloaked );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void SetBlindScreen( bool blind )
	{
		if ( Rpc.Caller is not { IsHost: true } ) return;
		Game.ActiveScene?.GetSystem<Systems.PlayerOverlaySystem>()?.SetBlind( blind );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void SpectateBody( Guid controllerId )
	{
		if ( Rpc.Caller is not { IsHost: true } ) return;
		Game.ActiveScene?.GetSystem<Systems.PlayerOverlaySystem>()?.Spectate( controllerId == Guid.Empty ? null : FindController( controllerId ) );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void PlaySoundForEveryone( string sound )
	{
		if ( Rpc.Caller is not { IsHost: true } || string.IsNullOrWhiteSpace( sound ) || sound.Length > 256 ) return;
		Sound.Play( sound );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void ChangeMapForEveryone( string map )
	{
		if ( Rpc.Caller is not { IsHost: true } || string.IsNullOrWhiteSpace( map ) || map.Length > 256 ) return;
		foreach ( var instance in Game.ActiveScene?.GetAllComponents<MapInstance>() ?? Enumerable.Empty<MapInstance>() ) instance.MapName = map;
	}

	private static PlayerController FindController( Guid id ) => Game.ActiveScene?.Directory.FindComponentByGuid( id ) as PlayerController;
}
