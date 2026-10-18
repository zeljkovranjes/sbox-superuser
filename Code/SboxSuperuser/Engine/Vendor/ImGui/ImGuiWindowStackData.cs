// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

internal struct ImGuiWindowStackData
{
	public ImGuiWindow Window;
	public ImGuiLastItemData ParentLastItemDataBackup;
	public int StackSizesColorStack;
	public int StackSizesStyleVarStack;
	public int StackSizesItemFlagsStack;
	public int StackSizesGroupStack;
	public int StackSizesBeginPopupStack;
	public int StackSizesDisabledStack;
	public ImGuiItemFlags BackupItemFlags;
}
