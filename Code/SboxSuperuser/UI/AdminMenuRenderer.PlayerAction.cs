using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string ActionPopup = "Player action";
	private static readonly string[] DurationPresets = { "30m", "2h", "1d", "7d", "permanent" };

	/// <summary>A moderation action waiting in the popup for its reason and, for timed restrictions, a duration.</summary>
	private sealed class ActionDraft
	{
		internal string SteamId = "";
		internal AdminAction? Action;
		internal AdminPlayerActionRow Module;
		internal string Reason = "";
		internal string Duration = "";
		internal string Error = "";

		internal string Label => Module?.Label ?? ActionLabel( Action ?? AdminAction.Kick );
		internal bool NeedsDuration => Module is not null ? !Module.Restore : Action?.IsTimed() == true;
		internal Vector4 Tone => Module is not null ? (Module.Restore ? AdminMenuTheme.Success : AdminMenuTheme.Danger) : AdminMenuTheme.Tone( Action ?? AdminAction.Kick );
	}

	private ActionDraft _actionDraft;

	/// <summary>Menu entries that open the action popup; shared by the player list, the player window and chat.</summary>
	private void ActionMenuItems( string steamId, IReadOnlyList<AdminAction> actions, IReadOnlyList<AdminPlayerActionRow> moduleActions )
	{
		foreach ( var action in actions )
		{
			ImGui.PushStyleColor( ImGuiCol.Text, AdminMenuTheme.Tone( action ) );
			var chosen = ImGui.MenuItem( ActionLabel( action ) + "..." );
			ImGui.PopStyleColor();
			if ( ImGui.IsItemHovered( ImGuiHoveredFlags.ForTooltip ) ) ImGui.SetTooltip( "{0}", ActionHint( action ) );
			if ( chosen ) StartAction( steamId, action, null );
		}
		foreach ( var module in moduleActions )
			if ( ImGui.MenuItem( module.Label + "..." ) ) StartAction( steamId, null, module );
	}

	private void StartAction( string steamId, AdminAction? action, AdminPlayerActionRow module )
	{
		// The model only accepts actions for the player whose details it holds.
		if ( action is not null && Model.Selected != steamId ) Model.Select( steamId );
		_actionDraft = new ActionDraft { SteamId = steamId, Action = action, Module = module, Duration = Model.Duration };
	}

	private void DrawActionPopup()
	{
		if ( _actionDraft is not null && !ImGui.IsPopupOpen( ActionPopup ) ) ImGui.OpenPopup( ActionPopup );

		var width = 420 * Scale;
		// Always, not Appearing: an auto-sized popup has no size on its first frame, so the pivot can't center it yet.
		ImGui.SetNextWindowPos( ImGui.GetIO().DisplaySize / 2, ImGuiCond.Always, new Vector2( 0.5f, 0.5f ) );
		// A fixed width keeps wrapped text from reflowing while the height fits itself; zero height means auto.
		ImGui.SetNextWindowSize( new Vector2( width + ImGui.GetStyle().WindowPadding.x * 2, 0 ), ImGuiCond.Always );
		var open = true;
		if ( !ImGui.BeginPopupModal( ActionPopup, ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings ) )
		{
			// The title-bar X closes the popup inside BeginPopupModal, which then returns false.
			if ( !open ) _actionDraft = null;
			return;
		}
		var draft = _actionDraft;
		if ( draft is null )
		{
			ImGui.CloseCurrentPopup();
			ImGui.EndPopup();
			return;
		}

		ImGui.TextColored( draft.Tone, "{0}", draft.Label );
		ImGui.SameLine();
		ImGui.TextUnformatted( NameOf( draft.SteamId ) ?? "Offline player" );
		ImGui.SameLine();
		ImGui.TextDisabled( "{0}", draft.SteamId );
		if ( draft.Action is { } hinted ) Caption( ActionHint( hinted ) );
		ImGui.Separator();

		ImGui.TextUnformatted( "Reason" );
		if ( ImGui.IsWindowAppearing() ) ImGui.SetKeyboardFocusHere();
		var submitted = DrawReason( ref draft.Reason, width );
		if ( draft.NeedsDuration )
		{
			ImGui.TextUnformatted( "Duration" );
			submitted |= DrawDuration( ref draft.Duration, width );
		}

		var refusal = Refusal( draft );
		if ( refusal.Length > 0 ) ColoredWrapped( refusal, AdminMenuTheme.Warning );
		if ( draft.Error.Length > 0 ) ColoredWrapped( draft.Error, AdminMenuTheme.Danger );
		ImGui.Separator();

		var ready = refusal.Length == 0 && draft.Reason.Trim().Length > 0 && (!draft.NeedsDuration || draft.Duration.Trim().Length > 0);
		var button = new Vector2( 120 * Scale, 0 );
		ImGui.BeginDisabled( !ready );
		ImGui.PushStyleColor( ImGuiCol.Text, draft.Tone );
		var confirmed = ImGui.Button( draft.Label, button ) || submitted && ready;
		ImGui.PopStyleColor();
		ImGui.EndDisabled();
		ImGui.SameLine();
		if ( ImGui.Button( "Cancel", button ) ) _actionDraft = null;
		else if ( confirmed && Submit( draft ) ) _actionDraft = null;

		if ( _actionDraft is null ) ImGui.CloseCurrentPopup();
		ImGui.EndPopup();
	}

	/// <summary>Why the draft can't be sent yet, or empty when it can.</summary>
	private string Refusal( ActionDraft draft )
	{
		if ( draft.Module is not null ) return Server.Running ? "Waiting for the previous command..." : "";
		if ( Model.Busy ) return "Waiting for the previous action...";
		var details = Model.Details;
		if ( Model.Selected != draft.SteamId || details is null ) return "Loading player...";
		if ( details.Status != CommandStatus.Success ) return details.Message;
		return details.Actions.Contains( draft.Action!.Value ) ? "" : $"You can't {draft.Label.ToLowerInvariant()} this player.";
	}

	/// <summary>Sends the action; returns false and keeps the popup open when the values are rejected.</summary>
	private bool Submit( ActionDraft draft )
	{
		var reason = draft.Reason.Trim();
		var duration = draft.NeedsDuration ? draft.Duration.Trim() : "";
		if ( draft.Module is { } module )
		{
			var arguments = module.Restore ? new[] { draft.SteamId, reason } : new[] { draft.SteamId, duration, reason };
			Server.Run( module.Command, arguments );
		}
		else
		{
			Model.Reason = reason;
			if ( duration.Length > 0 ) Model.Duration = duration;
			Model.Prepare( draft.Action!.Value );
			if ( Model.Confirmation is null )
			{
				draft.Error = Model.Feedback;
				return false;
			}
			Model.Confirm();
		}
		_preferences.Remember( reason, duration );
		return true;
	}

	/// <summary>Reason field with a dropdown of recently used reasons; returns true when Enter was pressed in it.</summary>
	private bool DrawReason( ref string reason, float width )
	{
		var recentWidth = ButtonWidth( "Recent" ) + ImGui.GetFrameHeight();
		var submitted = Field( "##reason", "Required, recorded in history", ref reason, 256,
			width - recentWidth - ImGui.GetStyle().ItemSpacing.x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		ImGui.SetNextItemWidth( recentWidth );
		ImGui.BeginDisabled( _preferences.RecentReasons.Count == 0 );
		if ( ImGui.BeginCombo( "##recent", "Recent" ) )
		{
			for ( var index = 0; index < _preferences.RecentReasons.Count; index++ )
			{
				// Reasons are user text, so the selectable label is an index; the text is drawn over it.
				ImGui.PushID( index );
				if ( ImGui.Selectable( "##reason", false, ImGuiSelectableFlags.AllowOverlap ) ) reason = _preferences.RecentReasons[ index ];
				ImGui.SameLine();
				ImGui.TextUnformatted( _preferences.RecentReasons[ index ] );
				ImGui.PopID();
			}
			ImGui.EndCombo();
		}
		ImGui.EndDisabled();
		return submitted;
	}

	/// <summary>Duration field followed by one button per preset; returns true when Enter was pressed in it.</summary>
	private static bool DrawDuration( ref string duration, float width )
	{
		var submitted = Field( "##duration", "e.g. 30m, 2h, 7d", ref duration, 32, width, ImGuiInputTextFlags.EnterReturnsTrue );
		foreach ( var preset in DurationPresets )
		{
			if ( preset != DurationPresets[ 0 ] ) ImGui.SameLine();
			var active = string.Equals( duration, preset, StringComparison.OrdinalIgnoreCase );
			if ( active ) ImGui.PushStyleColor( ImGuiCol.Button, ImGui.GetStyle().Colors[ (int)ImGuiCol.ButtonActive ] );
			if ( ImGui.SmallButton( preset ) ) duration = preset;
			if ( active ) ImGui.PopStyleColor();
		}
		return submitted;
	}
}
