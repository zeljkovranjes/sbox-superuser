using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
		| ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoSavedSettings;
	private static readonly string[] DurationPresets = { "30m", "2h", "1d", "7d", "permanent" };

	private void DrawWindow()
	{
		var size = new Vector2( MathF.Min( Screen.Width - 32, 1120 * Scale ), MathF.Min( Screen.Height - 32, 780 * Scale ) );
		ImGui.SetNextWindowSize( size );
		ImGui.SetNextWindowPos( new Vector2( (Screen.Width - size.x) / 2, (Screen.Height - size.y) / 2 ) );
		if ( ImGui.Begin( "Superuser", ref Open, WindowFlags ) )
		{
			DrawHeader();
			if ( Model.Feedback.Length > 0 )
				Banner( Model.Feedback, Model.FeedbackStatus is { } status ? AdminMenuTheme.Tone( status ) : AdminMenuTheme.Accent );
			ImGui.Spacing();
			if ( ImGui.BeginTabBar( "views" ) )
			{
				if ( ImGui.BeginTabItem( "Players" ) )
				{
					DrawPlayersTab();
					ImGui.EndTabItem();
				}
				if ( ImGui.BeginTabItem( "Activity" ) )
				{
					DrawActivity();
					ImGui.EndTabItem();
				}
				ImGui.EndTabBar();
			}
		}
		ImGui.End();
	}

	private void DrawHeader()
	{
		var scale = Scale;
		var mark = 36 * scale;
		var top = ImGui.GetCursorPosY();
		var origin = ImGui.GetCursorScreenPos();
		var draw = ImGui.GetWindowDrawList();
		draw.AddRectFilled( origin, origin + new Vector2( mark, mark ), ImGui.GetColorU32( AdminMenuTheme.Accent ), 9 * scale );
		ImGui.PushFont( null, AdminMenuTheme.TitleSize );
		draw.AddText( origin + (new Vector2( mark, mark ) - ImGui.CalcTextSize( "S" )) / 2, ImGui.GetColorU32( AdminMenuTheme.Text ), "S" );
		ImGui.PopFont();
		ImGui.Dummy( new Vector2( mark, mark ) );
		ImGui.SameLine( 0, 12 * scale );
		ImGui.BeginGroup();
		Heading( "Superuser", AdminMenuTheme.TitleSize );
		ImGui.PushFont( null, AdminMenuTheme.SmallSize );
		ImGui.TextColored( AdminMenuTheme.Muted, "{0}", "Player moderation" );
		ImGui.PopFont();
		ImGui.EndGroup();
		var headerHeight = MathF.Max( mark, ImGui.GetItemRectSize().y );

		var status = Model.Busy ? "Waiting for host" : "Connected";
		var spacing = ImGui.GetStyle().ItemSpacing.x;
		var controls = BadgeSize( status ).x + ButtonWidth( "Refresh" ) + ButtonWidth( "Close" ) + spacing * 2;
		ImGui.SameLine( ImGui.GetWindowContentRegionMax().x - controls );
		ImGui.SetCursorPosY( top + (headerHeight - BadgeSize( status ).y) / 2 );
		Badge( status, Model.Busy ? AdminMenuTheme.Warning : AdminMenuTheme.Success );
		ImGui.SameLine();
		ImGui.SetCursorPosY( top + (headerHeight - ImGui.GetFrameHeight()) / 2 );
		if ( ImGui.Button( "Refresh" ) ) Model.Refresh();
		ImGui.SetItemTooltip( "{0}", "Views also refresh every 5 seconds." );
		ImGui.SameLine();
		if ( ImGui.Button( "Close" ) ) Open = false;
		ImGui.SetItemTooltip( "{0}", "Close the menu (Esc)." );
		ImGui.SetCursorPosY( top + headerHeight );
		ImGui.Spacing();
	}

	private void DrawPlayersTab()
	{
		var available = ImGui.GetContentRegionAvail();
		ImGui.BeginChild( "players", new Vector2( MathF.Min( 330 * Scale, available.x * 0.36f ), available.y ), true );
		DrawPlayers();
		ImGui.EndChild();
		ImGui.SameLine();
		ImGui.BeginChild( "selected", new Vector2( 0, available.y ), true );
		DrawSelected();
		ImGui.EndChild();
	}

	private void DrawPlayers()
	{
		var style = ImGui.GetStyle();
		Label( "ONLINE PLAYERS" );
		var search = ButtonWidth( "Search" );
		var submit = Field( "##search", "Name or Steam ID", ref Model.Search, 256,
			ImGui.GetContentRegionAvail().x - search - style.ItemSpacing.x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		if ( ImGui.Button( "Search" ) || submit )
		{
			Model.PlayerPage = 0;
			Model.Refresh();
		}

		var players = Model.Players;
		var pager = players is { Status: CommandStatus.Success } && (Model.PlayerPage > 0 || players.HasMore);
		var footer = ImGui.GetFrameHeightWithSpacing() * (pager ? 3 : 2) + ImGui.GetTextLineHeightWithSpacing() + style.ItemSpacing.y * 2;
		ImGui.PushStyleColor( ImGuiCol.ChildBg, default( Vector4 ) );
		ImGui.BeginChild( "list", new Vector2( 0, MathF.Max( 80 * Scale, ImGui.GetContentRegionAvail().y - footer ) ) );
		if ( players is null ) Caption( "Loading players..." );
		else if ( players.Status != CommandStatus.Success ) Banner( players.Message, AdminMenuTheme.Tone( players.Status ) );
		else if ( players.Players.Length == 0 ) Caption( Model.Search.Length > 0 ? "No online players match this search." : "No players are online." );
		else foreach ( var player in players.Players ) DrawPlayerRow( player );
		ImGui.EndChild();
		ImGui.PopStyleColor();

		if ( pager )
		{
			ImGui.BeginDisabled( Model.PlayerPage == 0 );
			if ( ImGui.Button( "Previous" ) && Model.PlayerPage > 0 )
			{
				Model.PlayerPage--;
				Model.Refresh();
			}
			ImGui.EndDisabled();
			ImGui.SameLine();
			ImGui.AlignTextToFramePadding();
			ImGui.TextColored( AdminMenuTheme.Muted, "Page {0}", Model.PlayerPage + 1 );
			ImGui.SameLine( ImGui.GetWindowContentRegionMax().x - ButtonWidth( "Next" ) );
			ImGui.BeginDisabled( !players.HasMore );
			if ( ImGui.Button( "Next" ) )
			{
				Model.PlayerPage++;
				Model.Refresh();
			}
			ImGui.EndDisabled();
		}

		ImGui.Separator();
		Label( "OFFLINE LOOKUP" );
		var lookup = ButtonWidth( "Look up" );
		var submitLookup = Field( "##lookup", "17-digit Steam ID", ref Model.Lookup, 17,
			ImGui.GetContentRegionAvail().x - lookup - style.ItemSpacing.x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		if ( ImGui.Button( "Look up" ) || submitLookup ) Model.Select( Model.Lookup );
	}

	private void DrawPlayerRow( AdminPlayerRow player )
	{
		var scale = Scale;
		var pad = 8 * scale;
		var line = ImGui.GetTextLineHeight();
		ImGui.PushFont( null, AdminMenuTheme.SmallSize );
		var small = ImGui.GetTextLineHeight();
		ImGui.PopFont();
		var height = line + small + pad * 1.5f;
		var selected = Model.Selected == player.SteamId;
		var origin = ImGui.GetCursorScreenPos();

		// The exact Steam ID is the row's ID; the display name is drawn text only.
		if ( ImGui.Selectable( "##" + player.SteamId, selected, ImGuiSelectableFlags.None, new Vector2( 0, height ) ) ) Model.Select( player.SteamId );
		ImGui.SetItemTooltip( "{0}\n{1}", player.Name, player.SteamId );

		var draw = ImGui.GetWindowDrawList();
		var right = ImGui.GetItemRectMax().x;
		if ( selected ) draw.AddRectFilled( origin, new Vector2( origin.x + 3 * scale, origin.y + height ), ImGui.GetColorU32( AdminMenuTheme.Accent ), 2 * scale );
		var avatar = line + small - 2 * scale;
		var center = origin + new Vector2( avatar / 2 + 12 * scale, height / 2 );
		draw.AddCircleFilled( center, avatar / 2, ImGui.GetColorU32( selected ? AdminMenuTheme.Accent : AdminMenuTheme.Hover ) );
		var initial = Initial( player.Name );
		var glyph = ImGui.CalcTextSize( initial );
		draw.AddText( center - glyph / 2, ImGui.GetColorU32( AdminMenuTheme.Text ), initial );
		var text = new Vector2( origin.x + avatar + 24 * scale, origin.y + (height - line - small) / 2 );
		var clip = new ImRect( text, new Vector2( right - pad, origin.y + height ) );
		draw.AddText( 0, text, ImGui.GetColorU32( AdminMenuTheme.Text ), player.Name, 0, clip );
		ImGui.PushFont( null, AdminMenuTheme.SmallSize );
		draw.AddText( 0, text + new Vector2( 0, line ), ImGui.GetColorU32( AdminMenuTheme.Muted ), player.SteamId, 0, clip );
		ImGui.PopFont();
	}

	private static string Initial( string name )
	{
		foreach ( var character in name ?? "" )
			if ( char.IsLetterOrDigit( character ) ) return char.ToUpperInvariant( character ).ToString();
		return "?";
	}

	private void DrawSelected()
	{
		if ( Model.Selected.Length == 0 )
		{
			DrawEmpty( "No player selected", "Select a player, or look up an offline Steam ID." );
			return;
		}
		var name = Model.Players?.Players.FirstOrDefault( player => player.SteamId == Model.Selected )?.Name;
		Heading( name ?? "Steam ID " + Model.Selected, AdminMenuTheme.TitleSize );
		ImGui.PushFont( null, AdminMenuTheme.SmallSize );
		ImGui.TextColored( AdminMenuTheme.Muted, "{0}", Model.Selected );
		ImGui.PopFont();

		var details = Model.Details;
		if ( details is null ) { Caption( "Loading player..." ); return; }
		if ( details.Status != CommandStatus.Success ) { Banner( details.Message, AdminMenuTheme.Tone( details.Status ) ); return; }
		Badges( details.Roles.Select( role => (role, AdminMenuTheme.Accent) )
			.Prepend( details.Online ? ("Online", AdminMenuTheme.Success) : ("Offline", AdminMenuTheme.Neutral) ) );

		Section( "Active restrictions", details.Restrictions.Length );
		if ( details.Restrictions.Length == 0 ) Caption( "No active restrictions visible to you." );
		else DrawRecords( "restrictions", details.Restrictions, false );

		Section( "Take action", -1 );
		DrawActions( details );

		Section( "Moderation history", details.HistoryAllowed ? details.History.Length : -1 );
		if ( !details.HistoryAllowed ) Caption( "Requires admin.players.history." );
		else if ( details.History.Length == 0 ) Caption( "No moderation history on this page." );
		else DrawRecords( "history", details.History, true );
		if ( Model.HistoryPage > 0 || details.HasMore )
		{
			ImGui.BeginDisabled( Model.HistoryPage == 0 );
			if ( ImGui.Button( "Newer" ) && Model.HistoryPage > 0 )
			{
				Model.HistoryPage--;
				Model.Refresh();
			}
			ImGui.EndDisabled();
			ImGui.SameLine();
			ImGui.AlignTextToFramePadding();
			ImGui.TextColored( AdminMenuTheme.Muted, "Page {0}", Model.HistoryPage + 1 );
			ImGui.SameLine();
			ImGui.BeginDisabled( !details.HasMore );
			if ( ImGui.Button( "Older" ) )
			{
				Model.HistoryPage++;
				Model.Refresh();
			}
			ImGui.EndDisabled();
		}
	}

	private void DrawEmpty( string title, string body )
	{
		ImGui.SetCursorPosY( ImGui.GetCursorPosY() + ImGui.GetContentRegionAvail().y * 0.38f );
		Centered( title, AdminMenuTheme.Text, AdminMenuTheme.HeadingSize );
		Centered( body, AdminMenuTheme.Muted );
	}

	/// <summary>Section heading with an optional count; a negative count hides it.</summary>
	private void Section( string title, int count )
	{
		ImGui.Spacing();
		ImGui.Spacing();
		Heading( title );
		if ( count >= 0 )
		{
			ImGui.SameLine();
			ImGui.SetCursorPosY( ImGui.GetCursorPosY() + 2 * Scale );
			Badge( count.ToString(), AdminMenuTheme.Neutral );
		}
		ImGui.Separator();
	}

	private void DrawActions( AdminReply details )
	{
		if ( details.Actions.Length == 0 )
		{
			Caption( "No permitted actions for this target." );
			return;
		}
		var style = ImGui.GetStyle();
		var locked = Model.Busy || Model.Confirmation is not null;
		ImGui.BeginDisabled( locked );
		Label( "REASON" );
		Field( "##reason", "Required; recorded in moderation history", ref Model.Reason, 256, ImGui.GetContentRegionAvail().x );
		if ( details.Actions.Any( action => action is AdminAction.Ban or AdminAction.Mute ) )
		{
			Label( "DURATION (BAN AND TEXT MUTE)" );
			Field( "##duration", "e.g. 30m", ref Model.Duration, 32, 140 * Scale );
			foreach ( var preset in DurationPresets )
			{
				ImGui.SameLine( 0, style.ItemSpacing.x * 0.6f );
				var active = string.Equals( Model.Duration, preset, StringComparison.OrdinalIgnoreCase );
				if ( ToneButton( preset == "permanent" ? "Permanent" : preset, active ? AdminMenuTheme.Accent : AdminMenuTheme.Neutral ) ) Model.Duration = preset;
			}
		}
		ImGui.Spacing();

		var count = details.Actions.Length;
		var widest = details.Actions.Max( action => ButtonWidth( ActionLabel( action ) ) );
		var perRow = Math.Max( 1, Math.Min( count, (int)((ImGui.GetContentRegionAvail().x + style.ItemSpacing.x) / (widest + style.ItemSpacing.x)) ) );
		var width = (ImGui.GetContentRegionAvail().x - style.ItemSpacing.x * (perRow - 1)) / perRow;
		for ( var index = 0; index < count; index++ )
		{
			if ( index % perRow != 0 ) ImGui.SameLine();
			var action = details.Actions[ index ];
			if ( ToneButton( ActionLabel( action ), AdminMenuTheme.Tone( action ), new Vector2( width, ImGui.GetFrameHeight() * 1.2f ) ) ) Model.Prepare( action );
			ImGui.SetItemTooltip( "{0}", ActionHint( action ) );
		}
		ImGui.EndDisabled();
		DrawConfirmation();
	}

	private void DrawConfirmation()
	{
		var confirmation = Model.Confirmation;
		if ( confirmation is null ) return;
		var scale = Scale;
		var style = ImGui.GetStyle();
		var tone = AdminMenuTheme.Tone( confirmation.Action );
		var label = ActionLabel( confirmation.Action );
		var name = Model.Players?.Players.FirstOrDefault( player => player.SteamId == confirmation.SteamId )?.Name;
		var target = name is null ? confirmation.SteamId : $"{name}  ({confirmation.SteamId})";
		var pad = new Vector2( 16, 14 ) * scale;
		var key = pad.x + 80 * scale;
		var wrap = ImGui.GetContentRegionAvail().x - pad.x * 2 - key;
		ImGui.PushFont( null, AdminMenuTheme.HeadingSize );
		var heading = ImGui.GetTextLineHeightWithSpacing();
		ImGui.PopFont();
		var rows = ImGui.CalcTextSize( target, false, wrap ).y + ImGui.CalcTextSize( confirmation.Reason, false, wrap ).y
			+ (confirmation.Duration.Length > 0 ? ImGui.GetTextLineHeight() : 0) + style.ItemSpacing.y * 3;
		var height = pad.y * 2 + heading + rows + ImGui.GetFrameHeight() + style.ItemSpacing.y * 2;

		ImGui.Spacing();
		ImGui.PushStyleColor( ImGuiCol.ChildBg, AdminMenuTheme.Fade( tone, 0.08f ) );
		ImGui.PushStyleColor( ImGuiCol.Border, AdminMenuTheme.Fade( tone, 0.6f ) );
		ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, pad );
		ImGui.BeginChild( "confirm", new Vector2( 0, height ), ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding );
		ImGui.PushFont( null, AdminMenuTheme.HeadingSize );
		ImGui.TextColored( tone, "Confirm {0}?", label.ToLowerInvariant() );
		ImGui.PopFont();
		ImGui.PushTextWrapPos( 0 );
		ConfirmRow( "Target", target, key );
		ConfirmRow( "Reason", confirmation.Reason, key );
		if ( confirmation.Duration.Length > 0 ) ConfirmRow( "Duration", confirmation.Duration, key );
		ImGui.PopTextWrapPos();
		ImGui.Spacing();
		if ( ToneButton( "Confirm " + label.ToLowerInvariant(), tone, default, true ) ) Model.Confirm();
		ImGui.SameLine();
		if ( ImGui.Button( "Cancel" ) ) Model.Cancel();
		ImGui.EndChild();
		ImGui.PopStyleVar();
		ImGui.PopStyleColor( 2 );
	}

	private static void ConfirmRow( string key, string value, float width )
	{
		ImGui.TextColored( AdminMenuTheme.Muted, "{0}", key );
		ImGui.SameLine( width );
		ImGui.TextUnformatted( value );
	}

	private void DrawRecords( string id, AdminModerationRow[] rows, bool history )
	{
		var date = ImGui.CalcTextSize( "0000-00-00 00:00" ).x + ImGui.GetStyle().CellPadding.x * 2;
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.PadOuterX;
		if ( !ImGui.BeginTable( id, history ? 5 : 4, flags ) ) return;
		ImGui.TableSetupColumn( "Type", ImGuiTableColumnFlags.WidthFixed, BadgeSize( "Text mute" ).x + 8 * Scale );
		ImGui.TableSetupColumn( "Created (UTC)", ImGuiTableColumnFlags.WidthFixed, date );
		ImGui.TableSetupColumn( "Expires (UTC)", ImGuiTableColumnFlags.WidthFixed, date );
		if ( history ) ImGui.TableSetupColumn( "State", ImGuiTableColumnFlags.WidthFixed, BadgeSize( "Active" ).x + 8 * Scale );
		ImGui.TableSetupColumn( "Reason", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableHeadersRow();
		foreach ( var entry in rows )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			Badge( KindLabel( entry.Kind ), KindTone( entry.Kind ) );
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( Utc( entry.CreatedUtc ) );
			ImGui.TableNextColumn();
			if ( entry.Kind is "Ban" or "TextMute" ) ImGui.TextUnformatted( entry.ExpiresUtc is { } expires ? Utc( expires ) : "Permanent" );
			else if ( entry.Kind == "Kick" )
			{
				ImGui.TextColored( AdminMenuTheme.Warning, "{0}", "Intent only" );
				ImGui.SetItemTooltip( "{0}", "Kick intent; confirm its outcome in activity." );
			}
			else ImGui.TextColored( AdminMenuTheme.Muted, "{0}", "-" );
			if ( history )
			{
				ImGui.TableNextColumn();
				Badge( entry.Active ? "Active" : "Past", entry.Active ? AdminMenuTheme.Warning : AdminMenuTheme.Neutral );
			}
			ImGui.TableNextColumn();
			ImGui.TextWrapped( "{0}", entry.Reason );
		}
		ImGui.EndTable();
	}

	private void DrawActivity()
	{
		Heading( "Recent activity" );
		Caption( "The latest 20 audit records visible to you. Unknown outcomes are never success; inspect them before retrying." );
		ImGui.Spacing();
		var activity = Model.Activity;
		if ( activity is null ) { Caption( "Loading activity..." ); return; }
		if ( activity.Status != CommandStatus.Success ) Banner( activity.Message, AdminMenuTheme.Tone( activity.Status ) );
		if ( activity.Activity.Length == 0 )
		{
			if ( activity.Status == CommandStatus.Success ) Caption( "No recent activity visible to you." );
			return;
		}
		ImGui.BeginChild( "activity", new Vector2( 0, ImGui.GetContentRegionAvail().y ), true );
		var date = ImGui.CalcTextSize( "0000-00-00 00:00" ).x + ImGui.GetStyle().CellPadding.x * 2;
		if ( ImGui.BeginTable( "activity-table", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.PadOuterX ) )
		{
			ImGui.TableSetupColumn( "Time (UTC)", ImGuiTableColumnFlags.WidthFixed, date );
			ImGui.TableSetupColumn( "Actor", ImGuiTableColumnFlags.WidthStretch, 1.4f );
			ImGui.TableSetupColumn( "Command", ImGuiTableColumnFlags.WidthStretch, 1 );
			ImGui.TableSetupColumn( "Outcome", ImGuiTableColumnFlags.WidthFixed, BadgeSize( "StorageUnavailable" ).x + 8 * Scale );
			ImGui.TableSetupColumn( "Request", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "00000000..." ).x + ImGui.GetStyle().CellPadding.x * 2 );
			ImGui.TableHeadersRow();
			foreach ( var row in activity.Activity )
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.TextUnformatted( Utc( row.CreatedUtc ) );
				ImGui.TableNextColumn();
				ImGui.TextWrapped( "{0}", row.Actor );
				ImGui.TableNextColumn();
				ImGui.TextWrapped( "{0}", row.Command );
				ImGui.TableNextColumn();
				Badge( row.Outcome.ToString(), AdminMenuTheme.Tone( row.Outcome ) );
				ImGui.TableNextColumn();
				var request = row.RequestId.ToString( "N" );
				ImGui.TextColored( AdminMenuTheme.Muted, "{0}...", request.Substring( 0, 8 ) );
				ImGui.SetItemTooltip( "Request: {0}", request );
			}
			ImGui.EndTable();
		}
		ImGui.EndChild();
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

	private static string KindLabel( string kind ) => kind switch
	{
		"TextMute" => "Text mute",
		_ => kind
	};

	private static Vector4 KindTone( string kind ) => kind switch
	{
		"Ban" or "Kick" => AdminMenuTheme.Danger,
		"TextMute" or "Warn" => AdminMenuTheme.Warning,
		_ => AdminMenuTheme.Success
	};
}
