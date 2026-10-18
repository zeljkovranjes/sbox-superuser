// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

/// <summary>Callback for InputText() with the ImGuiInputTextFlags.Callback* flags. Return non-zero from a CharFilter callback to discard the character.</summary>
internal delegate int ImGuiInputTextCallback( ImGuiInputTextCallbackData data );
