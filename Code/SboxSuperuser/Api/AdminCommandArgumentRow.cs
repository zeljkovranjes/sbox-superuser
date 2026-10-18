namespace SboxSuperuser;

/// <summary>One argument of an AdminCommandRow.</summary>
public sealed class AdminCommandArgumentRow
{
	/// <summary>Gets the argument name.</summary>
	public string Name { get; init; } = "";

	/// <summary>Gets the argument's help text.</summary>
	public string Description { get; init; } = "";

	/// <summary>Gets the value kind, which decides the input widget.</summary>
	public CommandArgumentKind Kind { get; init; }

	/// <summary>Gets whether the argument may be left empty.</summary>
	public bool Optional { get; init; }

	/// <summary>Gets whether the argument takes the rest of the input, such as a reason.</summary>
	public bool Remainder { get; init; }

	/// <summary>Gets the inclusive numeric lower bound, if any.</summary>
	public double? Minimum { get; init; }

	/// <summary>Gets the inclusive numeric upper bound, if any.</summary>
	public double? Maximum { get; init; }

	/// <summary>Gets the allowed values for an Enum argument.</summary>
	public string[] Choices { get; init; } = Array.Empty<string>();
}
