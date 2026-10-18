// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImGuiNextItemDataFlags
{
	None = 0,
	HasWidth = 1 << 0,
	HasOpen = 1 << 1,
}
