// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal partial class ImGuiContext
{
	public readonly Dictionary<int, ImGuiTable> Tables = new();
	public readonly List<ImGuiTable> TablesStack = new();
	/// <summary>Draw lists currently split into channels by a table or legacy columns (channels can't nest).</summary>
	public readonly HashSet<ImDrawList> SplitDrawLists = new();
}
