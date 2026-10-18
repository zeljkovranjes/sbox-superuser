// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal struct ImGuiNextWindowData
{
	public ImGuiNextWindowDataFlags Flags;
	public ImGuiCond PosCond;
	public ImGuiCond SizeCond;
	public ImGuiCond CollapsedCond;
	public Vector2 PosVal;
	public Vector2 PosPivotVal;
	public Vector2 SizeVal;
	public Vector2 ContentSizeVal;
	public Vector2 ScrollVal;
	public ImGuiChildFlags ChildFlags;
	public bool CollapsedVal;
	public ImRect SizeConstraintRect;
	public Action<ImGuiSizeCallbackData> SizeCallback;
	public float BgAlphaVal;
	public Vector2 MenuBarOffsetMinVal;

	public void ClearFlags() => Flags = ImGuiNextWindowDataFlags.None;
}
