namespace SboxSuperuser;

/// <summary>One permission in a module's documentation.</summary>
public sealed class AdminPermissionRow
{
	/// <summary>Gets the exact permission key.</summary>
	public string Key { get; init; } = "";

	/// <summary>Gets what the permission allows, or empty when the module gave no description.</summary>
	public string Description { get; init; } = "";
}
