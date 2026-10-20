namespace SboxSuperuser;

/// <summary>One installed module in an About reply.</summary>
public sealed class AdminModuleRow
{
	/// <summary>Gets the module ID.</summary>
	public string Id { get; init; } = "";

	/// <summary>Gets the module API version it was built against.</summary>
	public int ApiVersion { get; init; }

	/// <summary>Gets the lifecycle state, such as Running or Failed.</summary>
	public string State { get; init; } = "";

	/// <summary>Gets safe diagnostic text without exception details.</summary>
	public string Message { get; init; } = "";

	/// <summary>Gets what the module does.</summary>
	public string Description { get; init; } = "";

	/// <summary>Gets the module's own release version, or empty when it declares none.</summary>
	public string Version { get; init; } = "";

	/// <summary>Gets who made the module, or empty.</summary>
	public string Author { get; init; } = "";

	/// <summary>Gets the module's source or homepage link, or empty.</summary>
	public string Website { get; init; } = "";

	/// <summary>Gets the titles of the module's written documentation pages, in order.</summary>
	public string[] Pages { get; init; } = Array.Empty<string>();

	/// <summary>Gets the permissions the module registers or its commands need.</summary>
	public AdminPermissionRow[] Permissions { get; init; } = Array.Empty<AdminPermissionRow>();

	/// <summary>Gets the modules this one depends on, such as "superuser.prefixes (API 1, optional)".</summary>
	public string[] Dependencies { get; init; } = Array.Empty<string>();
}
