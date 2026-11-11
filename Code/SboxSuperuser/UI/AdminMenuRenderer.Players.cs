using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const int SteamIdLength = 17;

	private bool _playersFocused;
	private bool _scrollToSelected;

	private static readonly AdminAction[] RowActions = { AdminAction.Kick, AdminAction.Warn, AdminAction.Mute, AdminAction.Gag, AdminAction.Ban };

	/// <summary>Opens this player's window, for links from outside the menu, optionally with an action's popup ready.</summary>
	internal void ShowPlayer( string steamId, AdminAction? action = null )
	{
		SelectPlayer( steamId );
		if ( action is { } chosen ) StartAction( steamId, chosen, null );
	}

	private void DrawPlayersWindow()
	{
		if ( !BeginWindow( PlayersWindow, "Players", new Vector2( 16, 16 ), new Vector2( 760, 560 ), new Vector2( 420, 300 ), out var expanded ) ) return;
		if ( expanded )
		{
			_playersFocused = ImGui.IsWindowFocused( ImGuiFocusedFlags.RootAndChildWindows );
			Caption( "Click a player to open them, or right-click for actions. Ctrl+F searches; Up and Down move through the list." );
			DrawSearch();
			DrawPlayerTable();
		}
		ImGui.End();
	}

	private void DrawSearch()
	{
		var style = ImGui.GetStyle();
		var io = ImGui.GetIO();
		if ( _playersFocused && io.KeyCtrl && ImGui.IsKeyPressed( ImGuiKey.F, false ) ) ImGui.SetKeyboardFocusHere();
		var submit = Field( "##search", "Name or Steam ID", ref Model.Search, 256,
			ImGui.GetContentRegionAvail().x - ButtonWidth( "Search" ) - style.ItemSpacing.x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		if ( ImGui.Button( "Search" ) || submit ) Search();
	}

	// A full Steam ID also opens that player directly, so offline players need no separate lookup.
	private void Search()
	{
		Model.PlayerPage = 0;
		Model.Refresh();
		var text = Model.Search.Trim();
		if ( text.Length == SteamIdLength && text.All( char.IsAsciiDigit ) ) SelectPlayer( text );
	}

	private void DrawPlayerTable()
	{
		var players = Model.Players;
		if ( players is null ) { Caption( "Loading players..." ); return; }
		if ( players.Status != CommandStatus.Success ) { ColoredWrapped( players.Message, AdminMenuTheme.Tone( players.Status ) ); return; }
		if ( players.Players.Length == 0 )
		{
			Caption( Model.Search.Length > 0 ? "No online players match this search." : "No players are online." );
			return;
		}

		MoveSelectionWithArrows( players.Players );

		// The table fills the window, leaving a row for the pager.
		var pager = Model.PlayerPage > 0 || players.HasMore ? ImGui.GetFrameHeightWithSpacing() : 0;
		var height = MathF.Max( 120 * Scale, ImGui.GetContentRegionAvail().y - pager );
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY;
		if ( ImGui.BeginTable( "players-table", 3, flags, new Vector2( 0, height ) ) )
		{
			ImGui.TableSetupScrollFreeze( 0, 1 );
			ImGui.TableSetupColumn( "Name", ImGuiTableColumnFlags.WidthStretch );
			ImGui.TableSetupColumn( "Steam ID", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "00000000000000000" ).x );
			ImGui.TableSetupColumn( "Warnings", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Warnings" ).x );
			ImGui.TableHeadersRow();
			foreach ( var player in players.Players )
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				// The exact Steam ID is the row's ID; the display name is drawn text only.
				ImGui.PushID( player.SteamId );
				var selected = Model.Selected == player.SteamId;
				if ( selected && _scrollToSelected )
				{
					ImGui.SetScrollHereY();
					_scrollToSelected = false;
				}
				if ( ImGui.Selectable( "##row", selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap ) )
					SelectPlayer( player.SteamId );
				DrawRowMenu( player );
				ImGui.PopID();
				ImGui.SameLine();
				ImGui.TextUnformatted( player.Name );
				ImGui.TableNextColumn();
				ImGui.TextDisabled( "{0}", player.SteamId );
				ImGui.TableNextColumn();
				// Repeat offenders stand out; zero stays quiet.
				if ( player.Warnings > 0 ) ImGui.TextColored( AdminMenuTheme.Warning, "{0}", player.Warnings );
			}
			ImGui.EndTable();
		}

		if ( Model.PlayerPage == 0 && !players.HasMore ) return;
		var change = Pager( "players", Model.PlayerPage, players.HasMore );
		if ( change == 0 ) return;
		Model.PlayerPage += change;
		Model.Refresh();
	}

	/// <summary>Up and Down move the selection while the window has focus and no text field is being edited.</summary>
	private void MoveSelectionWithArrows( AdminPlayerRow[] players )
	{
		if ( !_playersFocused || ImGui.GetIO().WantTextInput ) return;
		var step = ImGui.IsKeyPressed( ImGuiKey.DownArrow ) ? 1 : ImGui.IsKeyPressed( ImGuiKey.UpArrow ) ? -1 : 0;
		if ( step == 0 ) return;
		var current = Array.FindIndex( players, player => player.SteamId == Model.Selected );
		var next = current < 0 ? (step > 0 ? 0 : players.Length - 1) : Math.Clamp( current + step, 0, players.Length - 1 );
		if ( next == current ) return;
		SelectPlayer( players[ next ].SteamId );
		_scrollToSelected = true;
	}

	private void DrawRowMenu( AdminPlayerRow player )
	{
		if ( !ImGui.BeginPopupContextItem( "row-menu" ) ) return;
		if ( ImGui.MenuItem( "Copy Steam ID" ) ) Sandbox.UI.Clipboard.SetText( player.SteamId );
		if ( ImGui.MenuItem( "Copy name" ) ) Sandbox.UI.Clipboard.SetText( player.Name );
		ImGui.Separator();
		// The host has not said yet what this row allows; it refuses what the sender can't do.
		ActionMenuItems( player.SteamId, RowActions, Model.Players?.ModuleActions ?? Array.Empty<AdminPlayerActionRow>() );
		ImGui.EndPopup();
	}

	/// <summary>Selects the player and brings their window forward.</summary>
	private void SelectPlayer( string steamId )
	{
		if ( Model.Selected != steamId ) Model.Select( steamId );
		_layout.SetOpen( PlayerWindow, true );
		_focusPlayerWindow = true;
	}
}
