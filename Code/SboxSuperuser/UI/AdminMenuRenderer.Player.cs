using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string NoteCommand = "note";
	private const string SteamProfileUrl = "https://steamcommunity.com/profiles/";
	private string _note = "";
	private bool _focusPlayerWindow;

	/// <summary>The selected player in their own window: who they are, an actions menu, and tabs for the rest.</summary>
	private void DrawPlayerWindow()
	{
		if ( Model.Selected.Length == 0 ) return;
		// The first time it opens, it sits beside the player list rather than over it.
		var list = _layout.Window( PlayersWindow );
		var offset = list.HasBounds ? new Vector2( (list.X + list.Width) / Scale + 8, 16 ) : new Vector2( 790, 16 );
		if ( !BeginWindow( PlayerWindow, "Player###player", offset, new Vector2( 520, 560 ), new Vector2( 380, 300 ), out var expanded ) ) return;
		// Focus from inside: the vendored SetNextWindowFocus fails on a window's first frame.
		if ( _focusPlayerWindow && Interactive )
		{
			ImGui.SetWindowFocus();
			_focusPlayerWindow = false;
		}
		if ( expanded ) DrawPlayer();
		ImGui.End();
	}

	private void DrawPlayer()
	{
		var details = Model.Details;
		DrawPlayerHeader( details );
		ImGui.Separator();
		if ( details is null ) { Caption( "Loading player..." ); return; }
		if ( details.Status != CommandStatus.Success ) { ColoredWrapped( details.Message, AdminMenuTheme.Tone( details.Status ) ); return; }

		if ( !ImGui.BeginTabBar( "player-tabs" ) ) return;
		if ( ImGui.BeginTabItem( $"Restrictions ({details.Restrictions.Length})###restrictions" ) )
		{
			if ( details.Restrictions.Length == 0 ) Caption( "No active restrictions visible to you." );
			else DrawRecords( "restrictions", details.Restrictions, false );
			ImGui.EndTabItem();
		}
		if ( ImGui.BeginTabItem( details.HistoryAllowed ? $"History ({details.History.Length})###history" : "History###history" ) )
		{
			DrawHistory( details );
			ImGui.EndTabItem();
		}
		if ( ImGui.BeginTabItem( "Chat" ) )
		{
			DrawPlayerChat();
			ImGui.EndTabItem();
		}
		if ( Server.Can( NoteCommand ) && ImGui.BeginTabItem( "Notes" ) )
		{
			DrawNoteInput();
			ImGui.EndTabItem();
		}
		if ( Server.Can( AltsCommand ) && ImGui.BeginTabItem( "Alts" ) )
		{
			DrawLinkedAccounts( Model.Selected, false );
			ImGui.EndTabItem();
		}
		if ( Server.Can( "prefix.user.set" ) && ImGui.BeginTabItem( "Prefix" ) )
		{
			DrawPlayerPrefix();
			ImGui.EndTabItem();
		}
		ImGui.EndTabBar();
	}

	/// <summary>Name, Steam ID, online state and roles, with the actions menu on the right.</summary>
	private void DrawPlayerHeader( AdminReply details )
	{
		var actionsWidth = ButtonWidth( "Actions" ) + ImGui.GetFrameHeight();
		ImGui.TextUnformatted( NameOf( Model.Selected ) ?? "Offline player" );
		ImGui.SameLine();
		if ( SteamProfileLink( Model.Selected, null, "Click to open the Steam profile; right-click to copy the Steam ID." ) ) OpenSteamProfile( Model.Selected );
		if ( ImGui.IsItemClicked( ImGuiMouseButton.Right ) ) Sandbox.UI.Clipboard.SetText( Model.Selected );

		var loaded = details?.Status == CommandStatus.Success;
		if ( loaded )
		{
			ImGui.SameLine( ImGui.GetContentRegionMax().x - actionsWidth );
			ImGui.BeginDisabled( details.Actions.Length == 0 && details.ModuleActions.Length == 0 );
			if ( ImGui.Button( "Actions", new Vector2( actionsWidth, 0 ) ) ) ImGui.OpenPopup( "player-actions" );
			ImGui.EndDisabled();
			if ( ImGui.BeginPopup( "player-actions" ) )
			{
				ActionMenuItems( Model.Selected, details.Actions, details.ModuleActions );
				ImGui.EndPopup();
			}
		}
		if ( !loaded ) return;

		if ( details.Online ) ImGui.TextColored( AdminMenuTheme.Success, "{0}", "Online" );
		else ImGui.TextDisabled( "{0}", "Offline" );
		foreach ( var role in details.Roles )
		{
			ImGui.SameLine();
			ImGui.TextDisabled( "[{0}]", role );
		}
	}

	/// <summary>A Steam ID shown as a link, labelled with a name when known. Returns true when it was left-clicked.</summary>
	private static bool SteamProfileLink( string steamId, string label, string hint )
	{
		ImGui.TextColored( AdminMenuTheme.Link, "{0}", label ?? steamId );
		if ( ImGui.IsItemHovered() )
		{
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			ImGui.GetWindowDrawList().AddLine( new Vector2( min.x, max.y ), max, ImGui.GetColorU32( AdminMenuTheme.Link ) );
			ImGui.SetMouseCursor( ImGuiMouseCursor.Hand );
			ImGui.SetTooltip( "{0}", $"{SteamProfileUrl}{steamId}\n{hint}" );
		}
		return ImGui.IsItemClicked();
	}

	/// <summary>Opens the Steam profile; the engine asks before opening a browser.</summary>
	private static void OpenSteamProfile( string steamId ) => Game.Overlay.OpenUrl( SteamProfileUrl + steamId );

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
		if ( !ImGui.BeginTable( id, history ? 6 : 5, flags ) ) return;
		ImGui.TableSetupColumn( "Type", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Unmute" ).x );
		ImGui.TableSetupColumn( "Created (UTC)", ImGuiTableColumnFlags.WidthFixed, date );
		ImGui.TableSetupColumn( "Expires", ImGuiTableColumnFlags.WidthFixed, date );
		if ( history ) ImGui.TableSetupColumn( "State", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Active" ).x );
		ImGui.TableSetupColumn( "By", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "00000000000000000" ).x );
		ImGui.TableSetupColumn( "Reason", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableHeadersRow();
		for ( var row = 0; row < rows.Length; row++ )
		{
			var entry = rows[ row ];
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextColored( KindTone( entry.Kind ), "{0}", KindLabel( entry.Kind ) );
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( Utc( entry.CreatedUtc ) );
			ImGui.TableNextColumn();
			if ( entry.Active || entry.Kind is "Ban" or "TextMute" or "Gag" or "Gimp" ) DrawExpiry( entry.ExpiresUtc, entry.Active );
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
			DrawActor( entry.Actor, row );
			ImGui.TableNextColumn();
			ImGui.TextWrapped( "{0}", entry.Reason );
		}
		ImGui.EndTable();
	}

	/// <summary>Staff names open a menu on right-click: their Player window, their Steam profile, or copy the ID. The console and host services are plain text.</summary>
	private void DrawActor( string actor, int row )
	{
		if ( actor.Length != SteamIdLength || !actor.All( char.IsAsciiDigit ) ) { ImGui.TextDisabled( "{0}", actor ); return; }
		SteamProfileLink( actor, NameOf( actor ), "Right-click for options." );
		DrawActorMenu( actor, row );
	}

	/// <summary>The right-click menu on the item just drawn: open the player here or on Steam, or copy their ID.</summary>
	private void DrawActorMenu( string steamId, int row )
	{
		// One menu per row: the same player can appear on several rows.
		ImGui.PushID( row );
		if ( ImGui.BeginPopupContextItem( "actor-menu" ) )
		{
			if ( ImGui.MenuItem( "Open..." ) ) SelectPlayer( steamId );
			if ( ImGui.MenuItem( "Open Steam..." ) ) OpenSteamProfile( steamId );
			ImGui.Separator();
			if ( ImGui.MenuItem( "Copy Steam ID" ) ) Sandbox.UI.Clipboard.SetText( steamId );
			ImGui.EndPopup();
		}
		ImGui.PopID();
	}

	private static string ActionLabel( AdminAction action ) => action.ToString();

	private static string ActionHint( AdminAction action ) => action switch
	{
		AdminAction.Kick => "Disconnect this online player.",
		AdminAction.Warn => "Record a warning and notify the player if online.",
		AdminAction.Ban => "Ban this Steam ID for the duration, or permanently.",
		AdminAction.Unban => "Revoke the active ban.",
		AdminAction.Mute => "Stop their text chat for the duration, or permanently.",
		AdminAction.Unmute => "Revoke the active mute.",
		AdminAction.Gag => "Stop their voice chat for the duration, or permanently.",
		AdminAction.Ungag => "Revoke the active gag.",
		AdminAction.Gimp => "Replace their chat messages with silly lines for the duration, or permanently.",
		_ => "Revoke the active gimp."
	};

	// "TextMute" is the stored name; players and staff just call it a mute.
	private static string KindLabel( string kind ) => kind == "TextMute" ? "Mute" : kind;

	private static Vector4 KindTone( string kind ) => kind switch
	{
		"Ban" or "Kick" => AdminMenuTheme.Danger,
		"TextMute" or "Gag" or "Gimp" or "Warn" => AdminMenuTheme.Warning,
		"Note" => AdminMenuTheme.Neutral,
		_ => AdminMenuTheme.Success
	};
}
