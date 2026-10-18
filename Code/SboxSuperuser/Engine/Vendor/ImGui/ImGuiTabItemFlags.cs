// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiTabItemFlags
{
	None = 0,
	UnsavedDocument = 1 << 0,
	SetSelected = 1 << 1,
	NoCloseWithMiddleMouseButton = 1 << 2,
	NoPushId = 1 << 3,
	NoTooltip = 1 << 4,
	NoReorder = 1 << 5,
	Leading = 1 << 6,
	Trailing = 1 << 7,
	NoAssumedClosure = 1 << 8,
	// Internal
	Button = 1 << 21,
}
