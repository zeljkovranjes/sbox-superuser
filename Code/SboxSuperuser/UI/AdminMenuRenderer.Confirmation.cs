using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string ConfirmPopup = "Confirm action";

	/// <summary>Modal mirror of the model's pending confirmation; nothing reaches the host until Confirm.</summary>
	private void DrawConfirmation()
	{
		var confirmation = Model.Confirmation;
		if ( confirmation is not null && !ImGui.IsPopupOpen( ConfirmPopup ) ) ImGui.OpenPopup( ConfirmPopup );

		var center = new Vector2( Screen.Width, Screen.Height ) / 2;
		ImGui.SetNextWindowPos( center, ImGuiCond.Appearing, new Vector2( 0.5f, 0.5f ) );
		if ( !ImGui.BeginPopupModal( ConfirmPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings ) ) return;

		// The model can drop the confirmation on its own (Esc, a failed reply); follow it.
		if ( confirmation is null )
		{
			ImGui.CloseCurrentPopup();
			ImGui.EndPopup();
			return;
		}

		var label = ActionLabel( confirmation.Action );
		var name = Model.Players?.Players.FirstOrDefault( player => player.SteamId == confirmation.SteamId )?.Name;
		ImGui.TextColored( AdminMenuTheme.Tone( confirmation.Action ), "{0}", label + "?" );
		ImGui.Separator();
		ImGui.PushTextWrapPos( 420 * Scale );
		ConfirmRow( "Target", name is null ? confirmation.SteamId : $"{name}  ({confirmation.SteamId})" );
		ConfirmRow( "Reason", confirmation.Reason );
		if ( confirmation.Duration.Length > 0 ) ConfirmRow( "Duration", confirmation.Duration );
		ImGui.PopTextWrapPos();
		ImGui.Separator();

		var width = 120 * Scale;
		if ( ImGui.Button( "Confirm", new Vector2( width, 0 ) ) )
		{
			_preferences.Remember( confirmation.Reason, confirmation.Duration );
			Model.Confirm();
			ImGui.CloseCurrentPopup();
		}
		ImGui.SameLine();
		if ( ImGui.Button( "Cancel", new Vector2( width, 0 ) ) )
		{
			Model.Cancel();
			ImGui.CloseCurrentPopup();
		}
		ImGui.EndPopup();
	}

	private void ConfirmRow( string key, string value )
	{
		ImGui.TextDisabled( "{0}", key );
		ImGui.SameLine( 80 * Scale );
		ImGui.TextWrapped( "{0}", value );
	}
}
