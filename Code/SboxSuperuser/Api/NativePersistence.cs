namespace SboxSuperuser;

/// <summary>Chooses exactly one owner for an explicitly bound native setting.</summary>
public enum NativePersistence
{
	/// <summary>The value lasts only for the current engine session.</summary>
	Session,
	/// <summary>The engine's Saved flag owns persistence; the framework never stores a desired value.</summary>
	Engine,
	/// <summary>The framework stores and reapplies the value when the binding module is first started in a new runtime.</summary>
	Framework
}
