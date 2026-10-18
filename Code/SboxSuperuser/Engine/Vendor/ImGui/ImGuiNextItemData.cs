// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal struct ImGuiNextItemData
{
	public ImGuiNextItemDataFlags Flags;
	public ImGuiItemFlags ItemFlags;
	public float Width;
	public ImGuiCond OpenCond;
	public bool OpenVal;

	public void ClearFlags()
	{
		Flags = ImGuiNextItemDataFlags.None;
		ItemFlags = ImGuiItemFlags.None;
	}
}
