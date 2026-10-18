namespace SboxSuperuser;

/// <summary>One command the caller may run, with what an admin view needs to build its form.</summary>
public sealed class AdminCommandRow
{
	/// <summary>Gets the exact command key to pass to AdminCommandRequest.</summary>
	public string Name { get; init; } = "";

	/// <summary>Gets the command's help text.</summary>
	public string Description { get; init; } = "";

	/// <summary>Gets the module that registered it, such as superuser.core or a game's module.</summary>
	public string ModuleId { get; init; } = "";

	/// <summary>Gets whether the command changes state rather than only reading it.</summary>
	public bool Mutation { get; init; }

	/// <summary>Gets the arguments in the order the command expects them.</summary>
	public AdminCommandArgumentRow[] Arguments { get; init; } = Array.Empty<AdminCommandArgumentRow>();
}
