// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

/// <summary>
/// Data passed to a size constraint callback set by <see cref="ImGui.SetNextWindowSizeConstraints(Vector2, Vector2, Action{ImGuiSizeCallbackData})"/>.
/// </summary>
internal class ImGuiSizeCallbackData
{
	public Vector2 Pos;
	public Vector2 CurrentSize;
	public Vector2 DesiredSize;
}
