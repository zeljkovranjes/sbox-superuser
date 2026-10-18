// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal class ImGuiGroupData
{
	public int WindowID;
	public Vector2 BackupCursorPos;
	public Vector2 BackupCursorMaxPos;
	public Vector2 BackupCursorPosPrevLine;
	public float BackupIndent;
	public float BackupGroupOffset;
	public Vector2 BackupCurrLineSize;
	public float BackupCurrLineTextBaseOffset;
	public int BackupActiveIdIsAlive;
	public bool BackupActiveIdPreviousFrameIsAlive;
	public bool BackupHoveredIdIsAlive;
	public bool BackupIsSameLine;
	public bool EmitItem;
}
