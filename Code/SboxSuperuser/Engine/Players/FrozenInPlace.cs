using Sandbox;

namespace SboxSuperuser.Engine.Players;

/// <summary>Holds its owner's body where it was frozen every physics step; removing it lets them move again.</summary>
[Hide]
internal sealed class FrozenInPlace : Component
{
	internal Vector3 Anchor { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || GetComponent<PlayerController>() is not { } controller ) return;
		controller.WorldPosition = Anchor;
		controller.Body.Velocity = Vector3.Zero;
	}
}
