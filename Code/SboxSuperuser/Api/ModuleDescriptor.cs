using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Module ownership and compatibility metadata, not a loaded-module state.</summary>
public sealed class ModuleDescriptor
{
	/// <summary>Gets the stable module key.</summary>
	public string Id { get; }

	/// <summary>Gets operator-facing help.</summary>
	public string Description { get; }

	/// <summary>Gets the positive contract API version.</summary>
	public int ApiVersion { get; }

	/// <summary>Gets a snapshot of required and optional dependencies.</summary>
	public IReadOnlyList<ModuleDependency> Dependencies { get; }

	/// <summary>Gets whether ordinary staff must be prevented from disabling this security module.</summary>
	public bool IsRequiredSecurityModule { get; }

	/// <summary>Gets or sets the module's own release version shown in its documentation, such as "1.2.0".</summary>
	public string Version
	{
		get => _version;
		init => _version = Metadata( value, 32, nameof( Version ) );
	}

	/// <summary>Gets or sets who made the module, shown in its documentation.</summary>
	public string Author
	{
		get => _author;
		init => _author = Metadata( value, 64, nameof( Author ) );
	}

	/// <summary>Gets or sets a link for the module's source or homepage, shown in its documentation.</summary>
	public string Website
	{
		get => _website;
		init => _website = Metadata( value, 256, nameof( Website ) );
	}

	private readonly string _version = "";
	private readonly string _author = "";
	private readonly string _website = "";

	/// <summary>Creates metadata; duplicate, cycle and compatibility checks belong to the module manager.</summary>
	public ModuleDescriptor( string id, string description, int apiVersion,
		IEnumerable<ModuleDependency> dependencies = null, bool isRequiredSecurityModule = false )
	{
		Id = ContractGuard.Key( id, nameof( id ) );
		Description = ContractGuard.Text( description, nameof( description ) );
		if ( apiVersion < 1 )
		{
			throw new ArgumentOutOfRangeException( nameof( apiVersion ) );
		}

		ApiVersion = apiVersion;
		Dependencies = ContractGuard.Copy( dependencies ?? Array.Empty<ModuleDependency>(), nameof( dependencies ) );
		IsRequiredSecurityModule = isRequiredSecurityModule;
	}

	// Optional display text: empty is allowed, control characters and oversized values are not.
	private static string Metadata( string value, int maximumLength, string parameter )
	{
		value ??= "";
		if ( value.Length > maximumLength || value.Any( char.IsControl ) )
			throw new ArgumentException( $"Use printable text of at most {maximumLength} characters.", parameter );
		return value.Trim();
	}
}
