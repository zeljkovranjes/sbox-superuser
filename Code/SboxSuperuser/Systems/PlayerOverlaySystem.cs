using Sandbox;

namespace SboxSuperuser.Systems;

/// <summary>This machine's own blindness and spectating, both set by the host.</summary>
internal sealed class PlayerOverlaySystem : GameObjectSystem<PlayerOverlaySystem>
{
	private const float SpectateBack = 96;
	private const float SpectateUp = 16;

	private bool _blind;
	private PlayerController _watching;
	private PlayerController _ownCamera;

	public PlayerOverlaySystem( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 96, Update, nameof( PlayerOverlaySystem ) );

	internal void SetBlind( bool blind ) => _blind = blind;

	internal void Spectate( PlayerController target )
	{
		// Hand the camera back to our own controller before switching or stopping.
		if ( _ownCamera.IsValid() ) _ownCamera.UseCameraControls = true;
		_ownCamera = null;
		_watching = target;
		if ( target is null ) return;
		_ownCamera = Scene.GetAllComponents<PlayerController>().FirstOrDefault( controller => !controller.IsProxy );
		if ( _ownCamera.IsValid() ) _ownCamera.UseCameraControls = false;
	}

	private void Update()
	{
		if ( Scene.Camera is not { } camera ) return;
		if ( _watching is not null )
		{
			if ( !_watching.IsValid() ) Spectate( null );
			else
			{
				var eyes = _watching.EyeAngles.ToRotation();
				camera.WorldRotation = eyes;
				camera.WorldPosition = _watching.EyePosition - eyes.Forward * SpectateBack + Vector3.Up * SpectateUp;
			}
		}
		if ( !_blind ) return;
		using var hud = camera.BeginHud();
		hud.Fill = Color.Black;
		hud.Rect( new Rect( Vector2.Zero, camera.ScreenRect.Size ) );
	}
}
