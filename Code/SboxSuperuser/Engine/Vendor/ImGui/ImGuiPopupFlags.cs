// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiPopupFlags
{
	None = 0,
	MouseButtonLeft = 0,
	MouseButtonRight = 1,
	MouseButtonMiddle = 2,
	MouseButtonMask_ = 0x1F,
	MouseButtonDefault_ = 1,
	NoReopen = 1 << 5,
	NoOpenOverExistingPopup = 1 << 7,
	NoOpenOverItems = 1 << 8,
	AnyPopupId = 1 << 10,
	AnyPopupLevel = 1 << 11,
	AnyPopup = AnyPopupId | AnyPopupLevel,
}
