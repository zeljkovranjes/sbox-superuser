using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	/// <summary>Every active ban and text mute the caller may see, newest first, with revoke buttons.</summary>
	private void DrawRestrictionsWindow()
	{
		if ( !BeginWindow( RestrictionsWindow, "Bans & mutes", new Vector2( 56, 56 ), new Vector2( 760, 420 ), new Vector2( 420, 240 ), out var expanded ) ) return;
		if ( expanded )
		{
			Caption( "Every active ban and text mute you can see. Click a row to open the player." );
			DrawRestrictionSearch();
			DrawRestrictionTable();
		}
		ImGui.End();
	}

	private void DrawRestrictionSearch()
	{
		var style = ImGui.GetStyle();
		var submit = Field( "##restriction-search", "Filter by Steam ID", ref Server.RestrictionSearch, 17,
			ImGui.GetContentRegionAvail().x - ButtonWidth( "Search" ) - style.ItemSpacing.x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		if ( !ImGui.Button( "Search##restrictions" ) && !submit ) return;
		Server.RestrictionPage = 0;
		Server.RequestRestrictions( DateTimeOffset.UtcNow, true );
	}

	private void DrawRestrictionTable()
	{
		var reply = Server.Restrictions;
		if ( reply is null ) { Caption( "Loading..." ); return; }
		if ( reply.Status != CommandStatus.Success ) { ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) ); return; }
		if ( reply.Restrictions.Length == 0 )
		{
			Caption( Server.RestrictionSearch.Length > 0 ? "No active restrictions match this Steam ID." : "No active bans or text mutes." );
			return;
		}

		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY;
		var footer = reply.HasMore || Server.RestrictionPage > 0 ? ImGui.GetFrameHeightWithSpacing() : 0;
		if ( ImGui.BeginTable( "restrictions-table", 5, flags, new Vector2( 0, -footer ) ) )
		{
			ImGui.TableSetupScrollFreeze( 0, 1 );
			ImGui.TableSetupColumn( "Player", ImGuiTableColumnFlags.WidthStretch, 1.2f );
			ImGui.TableSetupColumn( "Type", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Text mute" ).x );
			ImGui.TableSetupColumn( "Expires", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "in 00d 00h" ).x );
			ImGui.TableSetupColumn( "Reason", ImGuiTableColumnFlags.WidthStretch, 1.6f );
			ImGui.TableSetupColumn( "##revoke", ImGuiTableColumnFlags.WidthFixed, ButtonWidth( "Unmute" ) );
			ImGui.TableHeadersRow();
			foreach ( var row in reply.Restrictions ) DrawRestrictionRow( row, reply.Actions );
			ImGui.EndTable();
		}

		if ( !reply.HasMore && Server.RestrictionPage == 0 ) return;
		var change = Pager( "restrictions", Server.RestrictionPage, reply.HasMore );
		if ( change == 0 ) return;
		Server.RestrictionPage += change;
		Server.RequestRestrictions( DateTimeOffset.UtcNow, true );
	}

	private void DrawRestrictionRow( AdminModerationRow row, AdminAction[] allowed )
	{
		ImGui.TableNextRow();
		ImGui.PushID( row.RequestId.ToString( "N" ) );
		ImGui.TableNextColumn();
		// The Steam ID is the row's ID; a name, when the player is online, is drawn text only.
		if ( ImGui.Selectable( "##open", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap ) ) OpenPlayer( row.SteamId );
		ImGui.SameLine();
		var name = NameOf( row.SteamId );
		if ( name is null ) ImGui.TextUnformatted( row.SteamId );
		else
		{
			ImGui.TextUnformatted( name );
			ImGui.SameLine();
			ImGui.TextDisabled( "{0}", row.SteamId );
		}
		ImGui.TableNextColumn();
		ImGui.TextColored( KindTone( row.Kind ), "{0}", KindLabel( row.Kind ) );
		ImGui.TableNextColumn();
		DrawExpiry( row.ExpiresUtc, true );
		ImGui.TableNextColumn();
		ImGui.TextWrapped( "{0}", row.Reason );
		ImGui.TableNextColumn();

		var revoke = row.Kind == "Ban" ? AdminAction.Unban : AdminAction.Unmute;
		if ( allowed.Contains( revoke ) && ImGui.SmallButton( ActionLabel( revoke ) ) )
		{
			RunCommand( $"{ActionLabel( revoke )} this player?", revoke.ToString().ToLowerInvariant(), new[] { row.SteamId },
				confirm: true, needsReason: true, danger: false,
				("Player", name is null ? row.SteamId : $"{name}  ({row.SteamId})"), ("Original reason", row.Reason) );
		}
		ImGui.PopID();
	}

	/// <summary>Selects a player and brings the Players window forward to show them.</summary>
	private void OpenPlayer( string steamId )
	{
		SelectPlayer( steamId );
		_layout.SetOpen( PlayersWindow, true );
		ImGui.SetWindowFocus( "Players" );
	}

	private string NameOf( string steamId ) =>
		Model.Players?.Players.FirstOrDefault( player => player.SteamId == steamId )?.Name;
}
