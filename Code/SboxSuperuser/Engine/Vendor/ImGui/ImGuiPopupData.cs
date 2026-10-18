// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal class ImGuiPopupData
{
	public int PopupId;
	public ImGuiWindow Window;
	public ImGuiWindow RestoreNavWindow;
	public int ParentNavLayer;
	public int OpenFrameCount;
	public int OpenParentId;
	public Vector2 OpenPopupPos;
	public Vector2 OpenMousePos;
}
