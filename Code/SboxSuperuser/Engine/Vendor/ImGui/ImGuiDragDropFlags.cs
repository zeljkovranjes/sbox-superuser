// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiDragDropFlags
{
	None = 0,
	SourceNoPreviewTooltip = 1 << 0,
	SourceNoDisableHover = 1 << 1,
	SourceNoHoldToOpenOthers = 1 << 2,
	SourceAllowNullID = 1 << 3,
	SourceExtern = 1 << 4,
	PayloadAutoExpire = 1 << 5,
	PayloadNoCrossContext = 1 << 6,
	PayloadNoCrossProcess = 1 << 7,
	AcceptBeforeDelivery = 1 << 10,
	AcceptNoDrawDefaultRect = 1 << 11,
	AcceptNoPreviewTooltip = 1 << 12,
	AcceptPeekOnly = AcceptBeforeDelivery | AcceptNoDrawDefaultRect,
}
