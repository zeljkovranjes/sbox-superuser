using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private void DrawWindow()
	{
		var size = new Vector2( MathF.Min( Screen.Width - 32, 1040 ), MathF.Min( Screen.Height - 32, 760 ) );
		ImGui.SetNextWindowSize( size );
		ImGui.SetNextWindowPos( new Vector2( (Screen.Width - size.x) / 2, (Screen.Height - size.y) / 2 ) );
		if ( ImGui.Begin( "Superuser", ref Open, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove ) )
		{
			ImGui.TextUnformatted( "PLAYER MODERATION" );
			ImGui.SameLine();
			if ( ImGui.Button( "Refresh" ) ) Model.Refresh();
			ImGui.SameLine();
			if ( ImGui.Button( "Close" ) ) Open = false;
			ImGui.TextWrapped( "{0}", Model.Feedback );
			ImGui.Separator();
			var height = MathF.Max( 180, ImGui.GetContentRegionAvail().y - 175 );
			ImGui.BeginChild( "players", new Vector2( MathF.Min( 310, size.x * 0.36f ), height ), true );
			DrawPlayers();
			ImGui.EndChild();
			ImGui.SameLine();
			ImGui.BeginChild( "selected", new Vector2( 0, height ), true );
			DrawSelected();
			ImGui.EndChild();
			ImGui.BeginChild( "activity", new Vector2( 0, 0 ), true );
			DrawActivity();
			ImGui.EndChild();
		}
		ImGui.End();
	}

	private void DrawPlayers()
	{
		ImGui.TextUnformatted( "Online players" );
		ImGui.InputText( "Search", ref Model.Search, 256 );
		if ( ImGui.Button( "Find" ) )
		{
			Model.PlayerPage = 0;
			Model.Refresh();
		}
		var players = Model.Players;
		if ( players is null ) ImGui.TextUnformatted( "Loading..." );
		else if ( players.Status != CommandStatus.Success ) ImGui.TextWrapped( "{0}", players.Message );
		else
		{
			foreach ( var player in players.Players )
			{
				// Display names are text, never ImGui IDs or format strings.
				ImGui.TextWrapped( "{0}", player.Name );
				if ( ImGui.Selectable( player.SteamId, Model.Selected == player.SteamId ) ) Model.Select( player.SteamId );
			}
			if ( Model.PlayerPage > 0 && ImGui.Button( "Previous players" ) )
			{
				Model.PlayerPage--;
				Model.Refresh();
			}
			if ( players.HasMore && ImGui.Button( "More players" ) )
			{
				Model.PlayerPage++;
				Model.Refresh();
			}
		}
		ImGui.Separator();
		ImGui.TextUnformatted( "Offline lookup" );
		ImGui.InputText( "Steam ID", ref Model.Lookup, 17 );
		if ( ImGui.Button( "Look up" ) ) Model.Select( Model.Lookup );
	}

	private void DrawSelected()
	{
		if ( Model.Selected.Length == 0 )
		{
			ImGui.TextWrapped( "Select a player or look up a Steam ID." );
			return;
		}
		ImGui.TextUnformatted( Model.Selected );
		var details = Model.Details;
		if ( details is null ) { ImGui.TextUnformatted( "Loading..." ); return; }
		if ( details.Status != CommandStatus.Success ) { ImGui.TextWrapped( "{0}", details.Message ); return; }
		ImGui.Text( "{0} | {1}", details.Online ? "Online" : "Offline", string.Join( ", ", details.Roles ) );
		ImGui.TextUnformatted( "Active restrictions visible to you" );
		if ( details.Restrictions.Length == 0 ) ImGui.TextUnformatted( "None visible." );
		foreach ( var entry in details.Restrictions ) DrawRecord( entry );
		ImGui.Separator();
		ImGui.InputText( "Reason", ref Model.Reason, 256 );
		ImGui.InputText( "Duration", ref Model.Duration, 32 );
		ImGui.TextUnformatted( "Ban/mute: 30m, 2h, 7d or permanent" );
		ImGui.BeginDisabled( Model.Busy || Model.Confirmation is not null );
		for ( var index = 0; index < details.Actions.Length; index++ )
		{
			if ( index % 3 != 0 ) ImGui.SameLine();
			var action = details.Actions[ index ];
			if ( ImGui.Button( action == AdminAction.Mute ? "Text mute" : action.ToString() ) ) Model.Prepare( action );
		}
		ImGui.EndDisabled();
		if ( details.Actions.Length == 0 ) ImGui.TextWrapped( "No permitted actions for this target." );
		DrawConfirmation();
		ImGui.Separator();
		ImGui.TextUnformatted( "Moderation history" );
		if ( !details.HistoryAllowed ) ImGui.TextWrapped( "Requires admin.players.history." );
		foreach ( var entry in details.History ) DrawRecord( entry );
		if ( Model.HistoryPage > 0 && ImGui.Button( "Previous history" ) )
		{
			Model.HistoryPage--;
			Model.Refresh();
		}
		if ( details.HasMore && ImGui.Button( "More history" ) )
		{
			Model.HistoryPage++;
			Model.Refresh();
		}
	}

	private void DrawConfirmation()
	{
		var confirmation = Model.Confirmation;
		if ( confirmation is null ) return;
		ImGui.Separator();
		ImGui.TextWrapped( "Confirm {0} for {1}", confirmation.Action, confirmation.SteamId );
		ImGui.TextWrapped( "Reason: {0}", confirmation.Reason );
		if ( confirmation.Duration.Length > 0 ) ImGui.Text( "Duration: {0}", confirmation.Duration );
		if ( ImGui.Button( "Confirm action" ) ) Model.Confirm();
		ImGui.SameLine();
		if ( ImGui.Button( "Cancel" ) ) Model.Cancel();
	}

	private static void DrawRecord( AdminModerationRow entry )
	{
		ImGui.TextWrapped( "{0} | {1:u} | {2}", entry.Kind, entry.CreatedUtc, entry.Active ? "active" : "history" );
		ImGui.TextWrapped( "{0}", entry.Reason );
		if ( entry.Kind is "Ban" or "TextMute" ) ImGui.Text( "Expires: {0}", entry.ExpiresUtc?.ToString( "u" ) ?? "permanent" );
		if ( entry.Kind == "Kick" ) ImGui.TextWrapped( "Kick intent; confirm its outcome in activity." );
	}

	private void DrawActivity()
	{
		ImGui.TextUnformatted( "Recent activity visible to you" );
		if ( Model.Activity is null ) { ImGui.TextUnformatted( "Loading..." ); return; }
		if ( Model.Activity.Status != CommandStatus.Success ) ImGui.TextWrapped( "{0}", Model.Activity.Message );
		foreach ( var row in Model.Activity.Activity )
		{
			ImGui.TextWrapped( "{0:u}  {1}  {2}: {3}", row.CreatedUtc, row.Actor, row.Command, row.Outcome );
			ImGui.Text( "Request: {0:N}", row.RequestId );
		}
	}
}
