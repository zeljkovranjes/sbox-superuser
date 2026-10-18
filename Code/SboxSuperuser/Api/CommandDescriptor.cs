using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Immutable registration metadata, shared by explicit registration and future discovery.</summary>
public sealed class CommandDescriptor
{
	/// <summary>Gets the stable command key, independent of the console or chat prefix.</summary>
	public string Name { get; }

	/// <summary>Gets caller-facing command help.</summary>
	public string Description { get; }

	/// <summary>Gets the exact permission required at execution time.</summary>
	public string Permission { get; }

	/// <summary>Gets the owning module key.</summary>
	public string ModuleId { get; }

	/// <summary>Gets a snapshot of alternate names; collision validation belongs to registration.</summary>
	public IReadOnlyList<string> Aliases { get; }

	/// <summary>Gets ordered argument metadata.</summary>
	public IReadOnlyList<CommandArgumentDescriptor> Arguments { get; }

	/// <summary>Gets whether this command requires mutation serialization.</summary>
	public CommandExecutionPolicy ExecutionPolicy { get; }

	/// <summary>Creates a descriptor only; it does not register a native or framework command.</summary>
	public CommandDescriptor( string name, string description, string permission, string moduleId,
		IEnumerable<CommandArgumentDescriptor> arguments = null, IEnumerable<string> aliases = null,
		CommandExecutionPolicy executionPolicy = CommandExecutionPolicy.Mutation )
	{
		Name = ContractGuard.Key( name, nameof( name ) );
		Description = ContractGuard.Text( description, nameof( description ) );
		Permission = ContractGuard.Key( permission, nameof( permission ) );
		ModuleId = ContractGuard.Key( moduleId, nameof( moduleId ) );
		Arguments = ContractGuard.Copy( arguments ?? Array.Empty<CommandArgumentDescriptor>(), nameof( arguments ) );
		Aliases = ContractGuard.Copy( aliases ?? Array.Empty<string>(), nameof( aliases ) );
		foreach ( var alias in Aliases )
		{
			ContractGuard.Key( alias, nameof( aliases ) );
		}

		ExecutionPolicy = ContractGuard.EnumValue( executionPolicy, nameof( executionPolicy ) );
	}
}
