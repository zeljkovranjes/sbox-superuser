// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiNextWindowDataFlags
{
	None = 0,
	HasPos = 1 << 0,
	HasSize = 1 << 1,
	HasContentSize = 1 << 2,
	HasCollapsed = 1 << 3,
	HasSizeConstraint = 1 << 4,
	HasFocus = 1 << 5,
	HasBgAlpha = 1 << 6,
	HasScroll = 1 << 7,
	HasChildFlags = 1 << 8,
}
