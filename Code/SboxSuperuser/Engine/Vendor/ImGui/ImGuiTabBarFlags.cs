// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiTabBarFlags
{
	None = 0,
	Reorderable = 1 << 0,
	AutoSelectNewTabs = 1 << 1,
	TabListPopupButton = 1 << 2,
	NoCloseWithMiddleMouseButton = 1 << 3,
	NoTabListScrollingButtons = 1 << 4,
	NoTooltip = 1 << 5,
	DrawSelectedOverline = 1 << 6,
	FittingPolicyResizeDown = 1 << 7,
	FittingPolicyScroll = 1 << 8,
	FittingPolicyMask_ = FittingPolicyResizeDown | FittingPolicyScroll,
	FittingPolicyDefault_ = FittingPolicyResizeDown,
}
