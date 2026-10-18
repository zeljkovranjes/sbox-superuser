namespace SboxSuperuser;

/// <summary>Metadata for a public static handler discovered through a module registration scope.</summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
public sealed class AdminCommandAttribute : Attribute
{
	/// <summary>Gets or sets the stable command key; required during registration.</summary>
	public string Name { get; set; }

	/// <summary>Gets or sets caller-facing help; required during registration.</summary>
	public string Description { get; set; }

	/// <summary>Gets or sets the exact permission; required during registration.</summary>
	public string Permission { get; set; }

	/// <summary>Gets or sets the owning module key; required during registration.</summary>
	public string ModuleId { get; set; }

	/// <summary>Gets or sets aliases whose collisions must be checked during registration.</summary>
	public string[] Aliases { get; set; } = Array.Empty<string>();

	/// <summary>Gets or sets the execution policy; conservatively defaults to mutation.</summary>
	public CommandExecutionPolicy ExecutionPolicy { get; set; } = CommandExecutionPolicy.Mutation;
}
