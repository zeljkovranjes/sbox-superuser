// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal partial class ImGuiContext
{
	public readonly Dictionary<int, ImGuiTabBar> TabBars = new();
	public readonly List<ImGuiTabBar> TabBarStack = new();
	public ImGuiTabBar CurrentTabBar => TabBarStack.Count > 0 ? TabBarStack[^1] : null;
}
