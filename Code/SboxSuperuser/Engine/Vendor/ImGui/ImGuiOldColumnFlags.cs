// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiOldColumnFlags
{
	None = 0,
	NoBorder = 1 << 0,
	NoResize = 1 << 1,
	NoPreserveWidths = 1 << 2,
	NoForceWithinWindow = 1 << 3,
	GrowParentContentsSize = 1 << 4,
}
