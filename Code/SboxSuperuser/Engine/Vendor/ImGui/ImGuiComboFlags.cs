// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiComboFlags
{
	None = 0,
	PopupAlignLeft = 1 << 0,
	HeightSmall = 1 << 1,
	HeightRegular = 1 << 2,
	HeightLarge = 1 << 3,
	HeightLargest = 1 << 4,
	NoArrowButton = 1 << 5,
	NoPreview = 1 << 6,
	WidthFitPreview = 1 << 7,
	HeightMask_ = HeightSmall | HeightRegular | HeightLarge | HeightLargest,
}
