namespace SboxSuperuser;

/// <summary>A module's window in the admin menu, filtered to what the viewer may use.</summary>
public sealed class AdminMenuWindowRow
{
	/// <summary>The module that added it.</summary>
	public string ModuleId { get; init; } = "";

	/// <summary>The window title, also its entry under the Modules menu.</summary>
	public string Title { get; init; } = "";

	/// <summary>The window's contents, top to bottom.</summary>
	public AdminMenuElementRow[] Elements { get; init; } = Array.Empty<AdminMenuElementRow>();
}
