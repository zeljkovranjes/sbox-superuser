using Sandbox;

namespace SboxSuperuser.Engine.Players;

/// <summary>
/// Applies a host-requested change on this machine. Movement only takes effect on the body's owner; ragdolls and
/// cloaks run everywhere so every player sees them.
/// </summary>
internal static class BodyEffects
{
	internal static void Apply( PlayerController controller, BodyEffect effect, Vector3 vector )
	{
		var owns = !controller.IsProxy;
		switch ( effect )
		{
			case BodyEffect.Teleport when owns:
				controller.WorldPosition = vector;
				controller.Body.Velocity = Vector3.Zero;
				controller.Transform.ClearInterpolation();
				break;
			case BodyEffect.Push when owns:
				controller.Body.Velocity += vector;
				break;
			case BodyEffect.Freeze when owns:
				Hold( controller, true );
				break;
			case BodyEffect.Unfreeze when owns:
				Hold( controller, false );
				break;
			case BodyEffect.Noclip when owns:
				controller.GetOrAddComponent<NoclipMoveMode>();
				break;
			case BodyEffect.Unnoclip when owns:
				controller.GetComponent<NoclipMoveMode>()?.Destroy();
				break;
			case BodyEffect.Ragdoll:
				Ragdoll( controller, owns );
				break;
			case BodyEffect.Unragdoll:
				Unragdoll( controller, owns );
				break;
		}
	}

	internal static void SetCloaked( PlayerController controller, bool cloaked )
	{
		foreach ( var renderer in controller.GameObject.Root.GetComponentsInChildren<ModelRenderer>( true ) ) renderer.Enabled = !cloaked;
	}

	private static void Ragdoll( PlayerController controller, bool owns )
	{
		if ( controller.Renderer is not { } renderer || renderer.GetComponent<ModelPhysics>() is not null ) return;
		var physics = renderer.AddComponent<ModelPhysics>();
		physics.Model = renderer.Model;
		physics.Renderer = renderer;
		if ( owns ) Hold( controller, true );
	}

	private static void Unragdoll( PlayerController controller, bool owns )
	{
		if ( controller.Renderer is not { } renderer || renderer.GetComponent<ModelPhysics>() is not { } physics ) return;
		// Stand up where the ragdoll's root body came to rest.
		var rest = physics.Bodies.FirstOrDefault().Component?.WorldPosition ?? controller.WorldPosition;
		physics.Destroy();
		renderer.LocalPosition = Vector3.Zero;
		renderer.LocalRotation = Rotation.Identity;
		if ( !owns ) return;
		Hold( controller, false );
		controller.WorldPosition = rest;
		controller.Transform.ClearInterpolation();
	}

	/// <summary>Pins the body and takes away input, or gives both back.</summary>
	private static void Hold( PlayerController controller, bool held )
	{
		controller.UseInputControls = !held;
		if ( held ) controller.GetOrAddComponent<FrozenInPlace>().Anchor = controller.WorldPosition;
		else controller.GetComponent<FrozenInPlace>()?.Destroy();
	}
}
