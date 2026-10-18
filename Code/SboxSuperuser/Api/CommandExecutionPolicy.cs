namespace SboxSuperuser;

/// <summary>Declares whether a handler mutates shared state; it grants no permission.</summary>
public enum CommandExecutionPolicy
{
	/// <summary>Observes state without mutations.</summary>
	ReadOnly = 0,

	/// <summary>Requires serialized mutations and execution-time authorization.</summary>
	Mutation = 1,
}
