// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiSelectableFlags
{
	None = 0,
	NoAutoClosePopups = 1 << 0,
	SpanAllColumns = 1 << 1,
	AllowDoubleClick = 1 << 2,
	Disabled = 1 << 3,
	AllowOverlap = 1 << 4,
	Highlight = 1 << 5,
	[Obsolete( "Use NoAutoClosePopups" )] DontClosePopups = NoAutoClosePopups,

	// Internal
	NoHoldingActiveID = 1 << 20,
	SelectOnClick = 1 << 22,
	SelectOnRelease = 1 << 23,
	SpanAvailWidth = 1 << 24,
	SetNavIdOnHover = 1 << 25,
	NoPadWithHalfSpacing = 1 << 26,
}
