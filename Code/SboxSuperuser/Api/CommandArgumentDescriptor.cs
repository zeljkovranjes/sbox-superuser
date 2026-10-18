using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Immutable argument metadata; registry validation and parsing remain separate.</summary>
public sealed class CommandArgumentDescriptor
{
	/// <summary>Gets the stable parameter name.</summary>
	public string Name { get; }

	/// <summary>Gets caller-facing help.</summary>
	public string Description { get; }

	/// <summary>Gets the required value shape.</summary>
	public CommandArgumentKind Kind { get; }

	/// <summary>Gets whether omission is allowed; optional parameters must be trailing.</summary>
	public bool IsOptional { get; }

	/// <summary>Gets whether the final string parameter consumes remaining text.</summary>
	public bool IsRemainder { get; }

	/// <summary>Gets an optional inclusive numeric lower bound.</summary>
	public double? Minimum { get; }

	/// <summary>Gets an optional inclusive numeric upper bound.</summary>
	public double? Maximum { get; }

	/// <summary>Gets the explicit enum choices; arbitrary CLR types are not transported.</summary>
	public IReadOnlyList<string> Choices { get; }

	/// <summary>Gets required online/protection checks for player arguments.</summary>
	public CommandTargetPolicy TargetPolicy { get; }

	/// <summary>Gets audit handling; values are redacted unless a safe non-text scalar is explicitly declared.</summary>
	public CommandArgumentAudit Audit { get; }

	/// <summary>Creates metadata without installing a parser or resolving a target.</summary>
	public CommandArgumentDescriptor( string name, string description, CommandArgumentKind kind,
		bool isOptional = false, bool isRemainder = false, double? minimum = null,
		double? maximum = null, IEnumerable<string> choices = null,
		CommandTargetPolicy targetPolicy = CommandTargetPolicy.None, CommandArgumentAudit audit = CommandArgumentAudit.Redacted )
	{
		Name = ContractGuard.Key( name, nameof( name ) );
		Description = ContractGuard.Text( description, nameof( description ) );
		Kind = ContractGuard.EnumValue( kind, nameof( kind ) );
		TargetPolicy = ContractGuard.EnumValue( targetPolicy, nameof( targetPolicy ) );
		Audit = ContractGuard.EnumValue( audit, nameof( audit ) );
		if ( (minimum.HasValue && !double.IsFinite( minimum.Value ))
			|| (maximum.HasValue && !double.IsFinite( maximum.Value )) || minimum > maximum )
		{
			throw new ArgumentException( "Bounds must be finite and ordered." );
		}

		IsOptional = isOptional;
		IsRemainder = isRemainder;
		Minimum = minimum;
		Maximum = maximum;
		Choices = ContractGuard.Copy( choices ?? Array.Empty<string>(), nameof( choices ) );
	}
}
