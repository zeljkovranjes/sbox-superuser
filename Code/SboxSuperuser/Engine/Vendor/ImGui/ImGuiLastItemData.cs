// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal struct ImGuiLastItemData
{
	public int ID;
	public ImGuiItemFlags InFlags;
	public ImGuiItemStatusFlags StatusFlags;
	public ImRect Rect;
	public ImRect NavRect;
	public ImRect DisplayRect;
	public ImRect ClipRect;
}
