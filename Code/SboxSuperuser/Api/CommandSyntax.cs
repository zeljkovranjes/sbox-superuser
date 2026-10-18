namespace SboxSuperuser;

/// <summary>Descriptor-generated syntax and constraints; transport integrations add their own root prefix.</summary>
public sealed class CommandSyntax
{
	/// <summary>Gets canonical syntax with required angle-bracket and optional square-bracket arguments.</summary>
	public string Syntax { get; }

	/// <summary>Gets caller-facing command help.</summary>
	public string Description { get; }

	/// <summary>Gets the exact permission required for execution.</summary>
	public string Permission { get; }

	/// <summary>Gets immutable per-argument descriptions and constraints in declaration order.</summary>
	public IReadOnlyList<string> Arguments { get; }

	internal CommandSyntax( string syntax, string description, string permission, IEnumerable<string> arguments )
	{
		Syntax = syntax;
		Description = description;
		Permission = permission;
		Arguments = Array.AsReadOnly( arguments.ToArray() );
	}
}
