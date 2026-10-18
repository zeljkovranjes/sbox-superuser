// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal partial class ImGuiContext
{
	public ImGuiInputTextState InputTextState = new();
	public int TempInputId;
	public bool InputTextNoMarkEdited;
	public bool InputTextClipboardHandledByPanel;
}
