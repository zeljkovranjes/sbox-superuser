using Sandbox;

namespace SboxSuperuser.Engine.Vendor.ImGui;

internal static class ImGuiHost
{
	internal static Func<bool, string> ClipboardCopyProvider { get; set; }
	internal static bool SimulateInput { get; set; }
	private static readonly HashSet<string> UnavailableKeys = new();

	internal static bool PollKeyDown( ImGuiKey key )
	{
		if ( !Game.IsPlaying || SimulateInput || key >= ImGuiKey.MouseLeft ) return false;
		var name = ImGuiKeyNames.ToSboxName( key );
		if ( name is null || UnavailableKeys.Contains( name ) ) return false;
		try
		{
			return Input.Keyboard.Down( name );
		}
		catch ( Exception )
		{
			UnavailableKeys.Add( name );
			return false;
		}
	}
}
