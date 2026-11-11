using Sandbox;

namespace SboxSuperuser.Engine.Props;

/// <summary>Removes props from the active scene by their object IDs.</summary>
internal sealed class ScenePropWorld : IPropWorld
{
	public int Remove( IReadOnlyList<Guid> objects )
	{
		var removed = 0;
		foreach ( var id in objects )
		{
			if ( Game.ActiveScene?.Directory.FindByGuid( id ) is not { } gameObject || !gameObject.IsValid() ) continue;
			gameObject.Destroy();
			removed++;
		}
		return removed;
	}
}
