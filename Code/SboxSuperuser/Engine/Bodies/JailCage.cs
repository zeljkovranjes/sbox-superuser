using Sandbox;

namespace SboxSuperuser.Engine.Bodies;

/// <summary>Builds a networked box of solid walls around a player; destroying the root frees them.</summary>
internal static class JailCage
{
	private const float Margin = 24;
	private const float WallThickness = 4;
	private const string WallModel = "models/dev/box.vmdl";
	private static readonly Color WallTint = new( 0.2f, 0.6f, 1f, 0.35f );

	internal static GameObject Build( Scene scene, Vector3 feet, float bodyRadius, float bodyHeight )
	{
		var half = bodyRadius + Margin;
		var height = bodyHeight + Margin;
		var root = scene.CreateObject();
		root.Name = "Superuser Jail";
		root.WorldPosition = feet;
		// Four walls, a floor and a roof, each a thin box centered on its face.
		Wall( root, new Vector3( half, 0, height / 2 ), new Vector3( WallThickness, half * 2, height ) );
		Wall( root, new Vector3( -half, 0, height / 2 ), new Vector3( WallThickness, half * 2, height ) );
		Wall( root, new Vector3( 0, half, height / 2 ), new Vector3( half * 2, WallThickness, height ) );
		Wall( root, new Vector3( 0, -half, height / 2 ), new Vector3( half * 2, WallThickness, height ) );
		Wall( root, new Vector3( 0, 0, -WallThickness / 2 ), new Vector3( half * 2, half * 2, WallThickness ) );
		Wall( root, new Vector3( 0, 0, height ), new Vector3( half * 2, half * 2, WallThickness ) );
		root.NetworkSpawn();
		return root;
	}

	private static void Wall( GameObject root, Vector3 center, Vector3 size )
	{
		var wall = new GameObject( root, true, "Wall" );
		wall.LocalPosition = center;
		var collider = wall.AddComponent<BoxCollider>();
		collider.Scale = size;
		collider.Static = true;
		var renderer = wall.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( WallModel );
		renderer.Tint = WallTint;
		// The dev box is 50 units on a side.
		wall.LocalScale = size / 50f;
		collider.Scale = new Vector3( 50 );
	}
}
