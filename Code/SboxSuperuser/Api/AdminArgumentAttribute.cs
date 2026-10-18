namespace SboxSuperuser;

/// <summary>Describes a handler parameter by its CLR name using TypeLibrary-visible method metadata.</summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = true, Inherited = false )]
public sealed class AdminArgumentAttribute : Attribute
{
	/// <summary>Gets the exact CLR parameter name this metadata describes.</summary>
	public string Parameter { get; }

	/// <summary>Gets or sets the stable argument key; defaults to the lowercase CLR parameter name.</summary>
	public string Name { get; set; }

	/// <summary>Gets or sets argument help; defaults to the parameter name.</summary>
	public string Description { get; set; }

	/// <summary>Gets or sets an inclusive numeric minimum; NaN means unspecified.</summary>
	public double Minimum { get; set; } = double.NaN;

	/// <summary>Gets or sets an inclusive numeric maximum; NaN means unspecified.</summary>
	public double Maximum { get; set; } = double.NaN;

	/// <summary>Gets or sets whether the final string parameter consumes remaining text.</summary>
	public bool IsRemainder { get; set; }

	/// <summary>Gets or sets the mandatory player target policy.</summary>
	public CommandTargetPolicy TargetPolicy { get; set; }

	/// <summary>Gets or sets audit handling; defaults to redaction.</summary>
	public CommandArgumentAudit Audit { get; set; }

	/// <summary>Names one handler parameter; metadata for unknown or repeated parameters is rejected.</summary>
	public AdminArgumentAttribute( string parameter )
	{
		Parameter = parameter;
	}
}
