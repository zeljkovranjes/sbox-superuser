using Sandbox;
using Sandbox.Movement;

namespace SboxSuperuser.Engine.Players;

/// <summary>Flying through walls: wins over every other mode while present, with gravity and collision off.</summary>
[Hide]
internal sealed class NoclipMoveMode : MoveMode
{
	private const int AlwaysWins = 100000;
	private const float SpeedMultiplier = 3;

	private bool _colliderWasEnabled;

	public override int Score( PlayerController controller ) => AlwaysWins;

	public override void OnModeBegin()
	{
		if ( Controller.ColliderObject is not { } collider ) return;
		_colliderWasEnabled = collider.Enabled;
		collider.Enabled = false;
	}

	public override void OnModeEnd( MoveMode next ) => Restore();

	protected override void OnDestroy() => Restore();

	private void Restore()
	{
		if ( Controller?.ColliderObject is { } collider ) collider.Enabled = _colliderWasEnabled;
		if ( Controller?.Body is { } body ) body.Gravity = true;
	}

	public override void UpdateRigidBody( Rigidbody body )
	{
		body.Gravity = false;
		body.LinearDamping = 0;
		body.AngularDamping = 1;
	}

	// Fly where you look; jump rises and duck sinks.
	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		var direction = Controller.EyeAngles.ToRotation() * input.ClampLength( 1 );
		if ( Input.Down( "jump" ) ) direction += Vector3.Up;
		if ( Input.Down( "duck" ) ) direction -= Vector3.Up;
		return direction.ClampLength( 1 ) * Controller.RunSpeed * SpeedMultiplier;
	}

	public override void AddVelocity() => Controller.Body.Velocity = Controller.WishVelocity;
}
