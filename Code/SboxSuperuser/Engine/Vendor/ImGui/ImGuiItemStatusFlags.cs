// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiItemStatusFlags
{
	None = 0,
	HoveredRect = 1 << 0,
	HasDisplayRect = 1 << 1,
	Edited = 1 << 2,
	ToggledSelection = 1 << 3,
	ToggledOpen = 1 << 4,
	HasDeactivated = 1 << 5,
	Deactivated = 1 << 6,
	HoveredWindow = 1 << 7,
	Visible = 1 << 8,
	HasClipRect = 1 << 9,
	Openable = 1 << 20,
	Opened = 1 << 21,
	Checkable = 1 << 22,
	Checked = 1 << 23,
	Inputable = 1 << 24,
}
