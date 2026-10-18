using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string RunPopup = "Confirm command";

	/// <summary>A command waiting for the user to confirm it, and optionally to give a reason that becomes its last argument.</summary>
	private sealed class PendingRun
	{
		internal string Title = "";
		internal string Command = "";
		internal string[] Arguments = Array.Empty<string>();
		internal (string Key, string Value)[] Details = Array.Empty<(string, string)>();
		internal bool NeedsReason;
		internal bool Danger;
		internal string Reason = "";
	}

	private PendingRun _pendingRun;

	/// <summary>Runs a command through the host; changes and anything needing a reason are confirmed first.</summary>
	private void RunCommand( string title, string command, string[] arguments, bool confirm,
		bool needsReason = false, bool danger = false, params (string Key, string Value)[] details )
	{
		if ( !confirm && !needsReason )
		{
			Server.Run( command, arguments );
			return;
		}
		_pendingRun = new PendingRun
		{
			Title = title, Command = command, Arguments = arguments, Details = details,
			NeedsReason = needsReason, Danger = danger
		};
	}

	private void DrawRunConfirmation()
	{
		if ( _pendingRun is not null && !ImGui.IsPopupOpen( RunPopup ) ) ImGui.OpenPopup( RunPopup );

		ImGui.SetNextWindowPos( new Vector2( Screen.Width, Screen.Height ) / 2, ImGuiCond.Appearing, new Vector2( 0.5f, 0.5f ) );
		if ( !ImGui.BeginPopupModal( RunPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings ) ) return;
		var run = _pendingRun;
		if ( run is null )
		{
			ImGui.CloseCurrentPopup();
			ImGui.EndPopup();
			return;
		}

		ImGui.TextColored( run.Danger ? AdminMenuTheme.Danger : AdminMenuTheme.Warning, "{0}", run.Title );
		ImGui.Separator();
		ImGui.PushTextWrapPos( 420 * Scale );
		foreach ( var (key, value) in run.Details ) ConfirmRow( key, value );
		ImGui.PopTextWrapPos();
		if ( run.NeedsReason )
		{
			if ( ImGui.IsWindowAppearing() ) ImGui.SetKeyboardFocusHere();
			Field( "##run-reason", "Reason (required, recorded in history)", ref run.Reason, 256, 420 * Scale );
		}
		ImGui.Separator();

		var width = 120 * Scale;
		var ready = !run.NeedsReason || run.Reason.Trim().Length > 0;
		ImGui.BeginDisabled( !ready || Server.Running );
		if ( ImGui.Button( "Confirm", new Vector2( width, 0 ) ) )
		{
			var arguments = run.NeedsReason ? run.Arguments.Append( run.Reason.Trim() ).ToArray() : run.Arguments;
			if ( run.NeedsReason ) _preferences.Remember( run.Reason, "" );
			Server.Run( run.Command, arguments );
			_pendingRun = null;
			ImGui.CloseCurrentPopup();
		}
		ImGui.EndDisabled();
		ImGui.SameLine();
		if ( ImGui.Button( "Cancel", new Vector2( width, 0 ) ) )
		{
			_pendingRun = null;
			ImGui.CloseCurrentPopup();
		}
		ImGui.EndPopup();
	}
}
