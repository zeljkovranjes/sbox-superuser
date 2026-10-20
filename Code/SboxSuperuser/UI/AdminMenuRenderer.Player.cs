using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private static readonly string[] DurationPresets = { "30m", "2h", "1d", "7d", "permanent" };

	private const string NoteCommand = "note";
	private string _note = "";

	// Set from a row's right-click menu: the action Enter in the reason field will ask to confirm.
	private AdminAction? _quickAction;
	private bool _focusReason;

	private void DrawPlayer()
	{
		if ( Model.Selected.Length == 0 )
		{
			Caption( "No player selected." );
			return;
		}

		var name = Model.Players?.Players.FirstOrDefault( player => player.SteamId == Model.Selected )?.Name;
		ImGui.TextUnformatted( name ?? "Offline player" );
		ImGui.SameLine();
		ImGui.TextDisabled( "{0}", Model.Selected );

		var details = Model.Details;
		if ( details is null ) { Caption( "Loading player..." ); return; }
		if ( details.Status != CommandStatus.Success ) { ColoredWrapped( details.Message, AdminMenuTheme.Tone( details.Status ) ); return; }

		if ( details.Online ) ImGui.TextColored( AdminMenuTheme.Success, "{0}", "Online" );
		else ImGui.TextDisabled( "{0}", "Offline" );
		foreach ( var role in details.Roles )
		{
			ImGui.SameLine();
			ImGui.TextDisabled( "[{0}]", role );
		}
		ImGui.Separator();

		if ( ImGui.CollapsingHeader( $"Active restrictions ({details.Restrictions.Length})###restrictions", ImGuiTreeNodeFlags.DefaultOpen ) )
		{
			if ( details.Restrictions.Length == 0 ) Caption( "No active restrictions visible to you." );
			else DrawRecords( "restrictions", details.Restrictions, false );
		}

		if ( _quickAction is not null ) ImGui.SetNextItemOpen( true );
		if ( ImGui.CollapsingHeader( "Actions", ImGuiTreeNodeFlags.DefaultOpen ) ) DrawActions( details );

		if ( Server.Can( NoteCommand ) && ImGui.CollapsingHeader( "Add a note" ) ) DrawNoteInput();
		if ( Server.Can( "prefix.user.set" ) && ImGui.CollapsingHeader( "Prefix" ) ) DrawPlayerPrefix();
		if ( ImGui.CollapsingHeader( "Recent chat" ) ) DrawPlayerChat();

		var historyLabel = details.HistoryAllowed ? $"History ({details.History.Length})###history" : "History###history";
		if ( ImGui.CollapsingHeader( historyLabel ) ) DrawHistory( details );
	}

	private void DrawActions( AdminReply details )
	{
		if ( details.Actions.Length == 0 )
		{
			Caption( "No permitted actions for this target." );
			return;
		}
		var quick = _quickAction is { } wanted && details.Actions.Contains( wanted ) ? _quickAction : null;
		if ( _quickAction is { } refused && quick is null )
		{
			ColoredWrapped( $"You can't {ActionLabel( refused ).ToLowerInvariant()} this player.", AdminMenuTheme.Warning );
			_quickAction = null;
		}
		else if ( quick is { } pending )
		{
			ColoredWrapped( $"{ActionLabel( pending )}: type a reason, then press Enter.", AdminMenuTheme.Tone( pending ) );
		}

		ImGui.BeginDisabled( Model.Busy || Model.Confirmation is not null );
		if ( DrawReason() && quick is { } submit )
		{
			_quickAction = null;
			Model.Prepare( submit );
		}
		if ( details.Actions.Any( action => action is AdminAction.Ban or AdminAction.Mute ) )
		{
			Field( "##duration", "e.g. 30m", ref Model.Duration, 32, 120 * Scale );
			ImGui.SameLine();
			ImGui.SetNextItemWidth( 120 * Scale );
			if ( ImGui.BeginCombo( "Duration", "Presets" ) )
			{
				foreach ( var preset in DurationPresets )
				{
					var active = string.Equals( Model.Duration, preset, StringComparison.OrdinalIgnoreCase );
					if ( ImGui.Selectable( preset, active ) ) Model.Duration = preset;
				}
				ImGui.EndCombo();
			}
		}

		for ( var index = 0; index < details.Actions.Length; index++ )
		{
			if ( index > 0 ) ImGui.SameLine();
			var action = details.Actions[ index ];
			ImGui.PushStyleColor( ImGuiCol.Text, AdminMenuTheme.Tone( action ) );
			if ( ImGui.Button( ActionLabel( action ) ) )
			{
				_quickAction = null;
				Model.Prepare( action );
			}
			ImGui.PopStyleColor();
			if ( ImGui.IsItemHovered( ImGuiHoveredFlags.ForTooltip | ImGuiHoveredFlags.AllowWhenDisabled ) ) ImGui.SetTooltip( "{0}", ActionHint( action ) );
		}
		ImGui.EndDisabled();
	}

	/// <summary>Reason field with a dropdown of recently used reasons; returns true when Enter was pressed in it.</summary>
	private bool DrawReason()
	{
		var style = ImGui.GetStyle();
		var recentWidth = ButtonWidth( "Recent" ) + ImGui.GetFrameHeight();
		if ( _focusReason )
		{
			ImGui.SetKeyboardFocusHere();
			_focusReason = false;
		}
		var submitted = Field( "##reason", "Reason (required, recorded in history)", ref Model.Reason, 256,
			ImGui.GetContentRegionAvail().x - recentWidth - style.ItemSpacing.x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		ImGui.SetNextItemWidth( recentWidth );
		ImGui.BeginDisabled( _preferences.RecentReasons.Count == 0 );
		if ( ImGui.BeginCombo( "##recent", "Recent" ) )
		{
			for ( var index = 0; index < _preferences.RecentReasons.Count; index++ )
			{
				// Reasons are user text, so the selectable label is an index; the text is drawn over it.
				ImGui.PushID( index );
				if ( ImGui.Selectable( "##reason", false, ImGuiSelectableFlags.AllowOverlap ) ) Model.Reason = _preferences.RecentReasons[ index ];
				ImGui.SameLine();
				ImGui.TextUnformatted( _preferences.RecentReasons[ index ] );
				ImGui.PopID();
			}
			ImGui.EndCombo();
		}
		ImGui.EndDisabled();
		return submitted;
	}

	/// <summary>Private staff note on the selected player; it is kept in their moderation history.</summary>
	private void DrawNoteInput()
	{
		Caption( "Notes are only visible to staff who can read moderation history. The player is not told." );
		var submitted = Field( "##note", "What should other staff know?", ref _note, 256,
			ImGui.GetContentRegionAvail().x - ButtonWidth( "Add note" ) - ImGui.GetStyle().ItemSpacing.x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		ImGui.BeginDisabled( _note.Trim().Length == 0 || Server.Running );
		if ( ImGui.Button( "Add note" ) || submitted && _note.Trim().Length > 0 )
		{
			Server.Run( NoteCommand, new[] { Model.Selected, _note.Trim() } );
			_note = "";
		}
		ImGui.EndDisabled();
		if ( Server.LastCommand == NoteCommand && Server.LastResult is { } result )
			ImGui.TextColored( AdminMenuTheme.Tone( result.Status ), "{0}", result.Status == CommandStatus.Success ? "Note saved." : result.Message );
	}

	private void DrawHistory( AdminReply details )
	{
		if ( !details.HistoryAllowed ) { Caption( "Requires admin.players.history." ); return; }
		if ( details.History.Length == 0 ) Caption( "No moderation history on this page." );
		else DrawRecords( "history", details.History, true );
		if ( Model.HistoryPage == 0 && !details.HasMore ) return;
		var change = Pager( "history", Model.HistoryPage, details.HasMore, "Newer", "Older" );
		if ( change == 0 ) return;
		Model.HistoryPage += change;
		Model.Refresh();
	}

	private void DrawRecords( string id, AdminModerationRow[] rows, bool history )
	{
		var date = ImGui.CalcTextSize( "0000-00-00 00:00" ).x;
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable;
		if ( !ImGui.BeginTable( id, history ? 5 : 4, flags ) ) return;
		ImGui.TableSetupColumn( "Type", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Text mute" ).x );
		ImGui.TableSetupColumn( "Created (UTC)", ImGuiTableColumnFlags.WidthFixed, date );
		ImGui.TableSetupColumn( "Expires", ImGuiTableColumnFlags.WidthFixed, date );
		if ( history ) ImGui.TableSetupColumn( "State", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Active" ).x );
		ImGui.TableSetupColumn( "Reason", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableHeadersRow();
		foreach ( var entry in rows )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextColored( KindTone( entry.Kind ), "{0}", KindLabel( entry.Kind ) );
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( Utc( entry.CreatedUtc ) );
			ImGui.TableNextColumn();
			if ( entry.Kind is "Ban" or "TextMute" ) DrawExpiry( entry.ExpiresUtc, entry.Active );
			else if ( entry.Kind == "Kick" )
			{
				ImGui.TextColored( AdminMenuTheme.Warning, "{0}", "Intent only" );
				ImGui.SetItemTooltip( "{0}", "Kick intent; confirm its outcome in activity." );
			}
			else ImGui.TextDisabled( "{0}", "-" );
			if ( history )
			{
				ImGui.TableNextColumn();
				if ( entry.Active ) ImGui.TextColored( AdminMenuTheme.Warning, "{0}", "Active" );
				else ImGui.TextDisabled( "{0}", "Past" );
			}
			ImGui.TableNextColumn();
			ImGui.TextWrapped( "{0}", entry.Reason );
		}
		ImGui.EndTable();
	}

	private static string ActionLabel( AdminAction action ) => action == AdminAction.Mute ? "Text mute" : action.ToString();

	private static string ActionHint( AdminAction action ) => action switch
	{
		AdminAction.Kick => "Disconnect this online player.",
		AdminAction.Warn => "Record a warning and notify the player if online.",
		AdminAction.Ban => "Ban this Steam ID for the duration, or permanently.",
		AdminAction.Unban => "Revoke the active ban.",
		AdminAction.Mute => "Restrict ordinary text chat for the duration.",
		_ => "Revoke the text restriction."
	};

	private static string KindLabel( string kind ) => kind == "TextMute" ? "Text mute" : kind;

	private static Vector4 KindTone( string kind ) => kind switch
	{
		"Ban" or "Kick" => AdminMenuTheme.Danger,
		"TextMute" or "Warn" => AdminMenuTheme.Warning,
		"Note" => AdminMenuTheme.Neutral,
		_ => AdminMenuTheme.Success
	};
}
