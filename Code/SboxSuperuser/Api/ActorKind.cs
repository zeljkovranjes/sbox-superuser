namespace SboxSuperuser;

/// <summary>Origin of a host-authenticated principal; no kind implies an owner role.</summary>
public enum ActorKind
{
	/// <summary>No authenticated identity.</summary>
	Unknown = 0,

	/// <summary>A Steam player authenticated by the engine.</summary>
	Player = 1,

	/// <summary>The verified local dedicated-console transport.</summary>
	DedicatedConsole = 2,

	/// <summary>A host-owned service with explicitly assigned capabilities.</summary>
	InternalService = 3,
}
