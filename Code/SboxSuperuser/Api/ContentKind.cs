namespace SboxSuperuser;

/// <summary>What kind of content a ban covers.</summary>
public enum ContentKind
{
	/// <summary>A map, by package ident such as facepunch.flatgrass or by .vmap path.</summary>
	Map,

	/// <summary>A cloud package, by ident such as author.pack.</summary>
	Addon,

	/// <summary>Something spawned, by prefab path, model path or component type name.</summary>
	Entity
}
