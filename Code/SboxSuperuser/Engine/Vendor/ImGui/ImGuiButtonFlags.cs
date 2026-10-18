// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiButtonFlags
{
	None = 0,
	MouseButtonLeft = 1 << 0,
	MouseButtonRight = 1 << 1,
	MouseButtonMiddle = 1 << 2,
	MouseButtonMask_ = MouseButtonLeft | MouseButtonRight | MouseButtonMiddle,
	EnableNav = 1 << 3,

	// Internal
	PressedOnClick = 1 << 4,
	PressedOnClickRelease = 1 << 5,
	PressedOnClickReleaseAnywhere = 1 << 6,
	PressedOnRelease = 1 << 7,
	PressedOnDoubleClick = 1 << 8,
	PressedOnDragDropHold = 1 << 9,
	FlattenChildren = 1 << 11,
	AllowOverlap = 1 << 12,
	AlignTextBaseLine = 1 << 15,
	NoKeyModsAllowed = 1 << 16,
	NoHoldingActiveId = 1 << 17,
	NoNavFocus = 1 << 18,
	NoHoveredOnFocus = 1 << 19,
	NoSetKeyOwner = 1 << 20,
	NoTestKeyOwner = 1 << 21,
	PressedOnMask_ = PressedOnClick | PressedOnClickRelease | PressedOnClickReleaseAnywhere | PressedOnRelease | PressedOnDoubleClick | PressedOnDragDropHold,
	PressedOnDefault_ = PressedOnClickRelease,
}
