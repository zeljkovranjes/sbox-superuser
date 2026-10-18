// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal partial class ImGuiContext
{
	public bool DragDropActive;
	public bool DragDropWithinSource;
	public bool DragDropWithinTarget;
	public ImGuiDragDropFlags DragDropSourceFlags;
	public int DragDropSourceFrameCount = -1;
	public int DragDropMouseButton = -1;
	public readonly ImGuiPayload DragDropPayload = new();
	public ImRect DragDropTargetRect;
	public ImRect DragDropTargetClipRect;
	public int DragDropTargetId;
	public ImGuiDragDropFlags DragDropAcceptFlags;
	public float DragDropAcceptIdCurrRectSurface = float.MaxValue;
	public int DragDropAcceptIdCurr;
	public int DragDropAcceptIdPrev;
	public int DragDropAcceptFrameCount = -1;
	public int DragDropHoldJustPressedId;
}
