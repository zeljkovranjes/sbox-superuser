namespace SboxSuperuser;

/// <summary>One server-resolved resource in a module catalog. The ID, not the display title, is passed to actions.</summary>
public sealed class AdminCatalogItem
{
	public string Id { get; set; } = "";
	public string Title { get; set; } = "";
	public string Icon { get; set; } = "";
	public bool Blocked { get; set; }
}
