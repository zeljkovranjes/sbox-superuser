namespace SboxSuperuser.Engine.Props;

/// <summary>The scene as prop protection sees it; the live adapter is ScenePropWorld.</summary>
internal interface IPropWorld
{
	/// <summary>Removes these objects from the scene, if they still exist; returns how many were removed.</summary>
	int Remove( IReadOnlyList<Guid> objects );
}
