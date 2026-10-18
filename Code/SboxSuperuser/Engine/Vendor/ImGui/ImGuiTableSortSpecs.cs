// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

/// <summary>
/// Sorting specifications for a table (often handling sort specs for a single column, occasionally more).
/// Obtained by calling TableGetSortSpecs(). When SpecsDirty is true you can sort your data, then set it back to false.
/// </summary>
internal class ImGuiTableSortSpecs
{
	public ImGuiTableColumnSortSpecs[] Specs = Array.Empty<ImGuiTableColumnSortSpecs>();
	public int SpecsCount;
	/// <summary>Set to true when specs have changed since last time! Use this to sort again, then clear the flag.</summary>
	public bool SpecsDirty;
}
