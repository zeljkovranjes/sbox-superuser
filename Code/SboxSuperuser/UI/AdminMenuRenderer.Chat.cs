using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private string _chatFilter = "";

	// Set while a selected player's "Recent chat" section is open, so chat keeps loading for it.
	private bool _wantPlayerChat;

	/// <summary>The last public chat lines, kept in server memory only, so staff see what was said before acting.</summary>
	private void DrawChatWindow()
	{
		if ( !BeginWindow( ChatWindow, "Chat", new Vector2( 176, 176 ), new Vector2( 720, 420 ), new Vector2( 420, 220 ), out var expanded ) ) return;
		if ( expanded )
		{
			Caption( "The last 100 public messages. Kept only in server memory and never saved; commands are not included." );
			Field( "##chat-filter", "Filter by name, Steam ID or text", ref _chatFilter, 64, ImGui.GetContentRegionAvail().x );
			var reply = Server.Chat;
			if ( reply is null ) Caption( "Loading chat..." );
			else if ( reply.Status != CommandStatus.Success ) ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) );
			else DrawChatTable( "chat-table", reply.Chat.Where( MatchesChatFilter ).ToArray(), true );
		}
		ImGui.End();
	}

	private bool MatchesChatFilter( AdminChatRow line )
	{
		var filter = _chatFilter.Trim();
		return filter.Length == 0
			|| line.SteamId.Contains( filter, StringComparison.Ordinal )
			|| line.Name.Contains( filter, StringComparison.OrdinalIgnoreCase )
			|| line.Message.Contains( filter, StringComparison.OrdinalIgnoreCase );
	}

	/// <summary>Chat lines newest first; the player column is shown only when lines come from several players.</summary>
	private void DrawChatTable( string id, AdminChatRow[] lines, bool showPlayer )
	{
		if ( lines.Length == 0 ) { Caption( "No messages." ); return; }
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY;
		if ( !ImGui.BeginTable( id, showPlayer ? 3 : 2, flags, new Vector2( 0, showPlayer ? 0 : 160 * Scale ) ) ) return;
		ImGui.TableSetupScrollFreeze( 0, 1 );
		ImGui.TableSetupColumn( "Time (UTC)", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Time (UTC)" ).x );
		if ( showPlayer ) ImGui.TableSetupColumn( "Player", ImGuiTableColumnFlags.WidthFixed, 160 * Scale );
		ImGui.TableSetupColumn( "Message", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableHeadersRow();
		for ( var index = 0; index < lines.Length; index++ )
		{
			var line = lines[ index ];
			ImGui.TableNextRow();
			ImGui.PushID( index );
			ImGui.TableNextColumn();
			// Names and messages are player text, so the row ID is its index and the text is drawn over it.
			if ( ImGui.Selectable( "##line", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap ) && showPlayer )
				OpenPlayer( line.SteamId );
			DrawChatLineMenu( line );
			ImGui.SameLine();
			ImGui.TextUnformatted( line.CreatedUtc.UtcDateTime.ToString( "HH:mm:ss" ) );
			ImGui.SetItemTooltip( "{0}", Utc( line.CreatedUtc ) );
			if ( showPlayer )
			{
				ImGui.TableNextColumn();
				ImGui.TextUnformatted( line.Name );
				ImGui.SetItemTooltip( "{0}", line.SteamId );
			}
			ImGui.TableNextColumn();
			ImGui.TextWrapped( "{0}", line.Message );
			ImGui.PopID();
		}
		ImGui.EndTable();
	}

	private void DrawChatLineMenu( AdminChatRow line )
	{
		if ( !ImGui.BeginPopupContextItem( "line-menu" ) ) return;
		if ( ImGui.MenuItem( "Copy message" ) ) Sandbox.UI.Clipboard.SetText( line.Message );
		if ( ImGui.MenuItem( "Copy Steam ID" ) ) Sandbox.UI.Clipboard.SetText( line.SteamId );
		ImGui.Separator();
		if ( ImGui.MenuItem( "Open player" ) ) OpenPlayer( line.SteamId );
		if ( ImGui.MenuItem( "Text mute..." ) )
		{
			_layout.SetOpen( PlayersWindow, true );
			StartQuickAction( line.SteamId, AdminAction.Mute );
		}
		ImGui.EndPopup();
	}

	/// <summary>The selected player's own recent lines, inside the player details.</summary>
	private void DrawPlayerChat()
	{
		_wantPlayerChat = true;
		var reply = Server.Chat;
		if ( reply is null ) { Caption( "Loading chat..." ); return; }
		if ( reply.Status != CommandStatus.Success ) { ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) ); return; }
		DrawChatTable( "player-chat", reply.Chat.Where( line => line.SteamId == Model.Selected ).ToArray(), false );
	}
}
