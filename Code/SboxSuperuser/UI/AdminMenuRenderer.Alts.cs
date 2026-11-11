using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const float SuspectPaneShare = 0.42f;

	/// <summary>The alt-account dashboard: suspects on the left, riskiest first; the chosen one's linked accounts on the right.</summary>
	private void DrawAltsWindow()
	{
		if ( !BeginWindow( AltsWindow, "Alt accounts", new Vector2( 90, 70 ), new Vector2( 860, 480 ), new Vector2( 560, 300 ), out var expanded ) ) return;
		if ( expanded )
		{
			Caption( "Accounts that share a family-share copy, an IP address or a Steam party. Links to banned accounts rank first." );
			var height = ImGui.GetContentRegionAvail().y;
			ImGui.BeginChild( "suspects", new Vector2( ImGui.GetContentRegionAvail().x * SuspectPaneShare, height ), ImGuiChildFlags.Borders );
			DrawSuspects();
			ImGui.EndChild();
			ImGui.SameLine();
			ImGui.BeginChild( "linked", new Vector2( 0, height ), ImGuiChildFlags.Borders );
			DrawLinkedAccounts( Server.AltsOf, true );
			ImGui.EndChild();
		}
		ImGui.End();
	}

	private void DrawSuspects()
	{
		var reply = Server.AltSuspects;
		if ( reply is null ) { Caption( "Loading..." ); return; }
		if ( reply.Status != CommandStatus.Success ) { ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) ); return; }
		if ( reply.Alts.Length == 0 ) { Caption( "No linked accounts yet. Superuser records each player as they join." ); return; }
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;
		if ( ImGui.BeginTable( "suspect-table", 3, flags ) )
		{
			ImGui.TableSetupColumn( "Player", ImGuiTableColumnFlags.WidthStretch );
			ImGui.TableSetupColumn( "Risk", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Ban evasion?" ).x );
			ImGui.TableSetupColumn( "Alts", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Alts" ).x + 8 );
			ImGui.TableHeadersRow();
			foreach ( var suspect in reply.Alts )
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.PushID( suspect.SteamId );
				if ( ImGui.Selectable( "##suspect", Server.AltsOf == suspect.SteamId, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap ) )
					Server.RequestAlts( suspect.SteamId );
				ImGui.SameLine();
				PlayerLabel( suspect );
				ImGui.TableNextColumn();
				var (risk, tone) = Risk( suspect );
				ImGui.TextColored( tone, "{0}", risk );
				ImGui.TableNextColumn();
				ImGui.TextUnformatted( suspect.AltCount.ToString() );
				ImGui.PopID();
			}
			ImGui.EndTable();
		}
		if ( Server.SuspectPage == 0 && !reply.HasMore ) return;
		var change = Pager( "suspects", Server.SuspectPage, reply.HasMore );
		if ( change == 0 ) return;
		Server.SuspectPage += change;
		Server.RequestAltSuspects( DateTimeOffset.UtcNow, true );
	}

	/// <summary>The accounts linked to one player, with why; also the Player window's Alts tab.</summary>
	private void DrawLinkedAccounts( string steamId, bool heading )
	{
		if ( steamId.Length == 0 ) { Caption( "Choose a suspect to see their linked accounts." ); return; }
		if ( Server.AltsOf != steamId ) Server.RequestAlts( steamId );
		if ( heading )
		{
			ImGui.TextUnformatted( Server.AltSuspects?.Alts.FirstOrDefault( row => row.SteamId == steamId )?.Name ?? steamId );
			ImGui.SameLine();
			if ( SteamProfileLink( steamId, null, "Click to open the Steam profile." ) ) OpenSteamProfile( steamId );
			ImGui.SameLine( ImGui.GetContentRegionMax().x - ButtonWidth( "Open player" ) );
			if ( ImGui.SmallButton( "Open player" ) ) SelectPlayer( steamId );
			ImGui.Separator();
		}
		var reply = Server.Alts;
		if ( reply is null ) { Caption( "Loading..." ); return; }
		if ( reply.Status != CommandStatus.Success ) { ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) ); return; }
		DrawAddresses( reply.Addresses );
		if ( reply.Alts.Length == 0 ) { Caption( "No linked accounts." ); return; }
		ImGui.SeparatorText( "Linked accounts" );
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
		if ( !ImGui.BeginTable( "linked-table", 3, flags ) ) return;
		ImGui.TableSetupColumn( "Account", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableSetupColumn( "Why", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableSetupColumn( "Last seen", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "0000-00-00" ).x );
		ImGui.TableHeadersRow();
		for ( var row = 0; row < reply.Alts.Length; row++ )
		{
			var alt = reply.Alts[ row ];
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			PlayerLabel( alt );
			DrawActorMenu( alt.SteamId, row );
			ImGui.TableNextColumn();
			for ( var index = 0; index < alt.Evidence.Length; index++ )
			{
				if ( index > 0 ) ImGui.SameLine();
				ImGui.TextColored( EvidenceTone( alt.Evidence[ index ] ), "{0}", alt.Evidence[ index ] );
			}
			ImGui.TableNextColumn();
			ImGui.TextDisabled( "{0}", alt.LastSeenUtc == default ? "never" : alt.LastSeenUtc.ToString( "yyyy-MM-dd" ) );
		}
		ImGui.EndTable();
	}

	/// <summary>The player's IP addresses, newest first; only sent to staff with admin.alts.ips.</summary>
	private static void DrawAddresses( AdminAddressRow[] addresses )
	{
		if ( addresses.Length == 0 ) return;
		ImGui.SeparatorText( "IP history" );
		if ( !ImGui.BeginTable( "addresses", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp ) ) return;
		ImGui.TableSetupColumn( "Address", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableSetupColumn( "First seen", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "0000-00-00 00:00" ).x );
		ImGui.TableSetupColumn( "Last seen", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "0000-00-00 00:00" ).x );
		ImGui.TableHeadersRow();
		foreach ( var address in addresses )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( address.Address );
			if ( ImGui.IsItemClicked( ImGuiMouseButton.Right ) ) Sandbox.UI.Clipboard.SetText( address.Address );
			ImGui.SetItemTooltip( "{0}", "Right-click to copy" );
			ImGui.TableNextColumn();
			ImGui.TextDisabled( "{0}", address.FirstSeenUtc.ToString( "yyyy-MM-dd HH:mm" ) );
			ImGui.TableNextColumn();
			ImGui.TextDisabled( "{0}", address.LastSeenUtc.ToString( "yyyy-MM-dd HH:mm" ) );
		}
		ImGui.EndTable();
	}

	/// <summary>Name with online and banned markers.</summary>
	private static void PlayerLabel( AdminAltRow row )
	{
		ImGui.TextColored( row.Online ? AdminMenuTheme.Success : AdminMenuTheme.Neutral, "{0}", "●" );
		ImGui.SameLine();
		ImGui.TextUnformatted( row.Name );
		if ( !row.Banned ) return;
		ImGui.SameLine();
		ImGui.TextColored( AdminMenuTheme.Danger, "{0}", "banned" );
	}

	private static (string Label, Vector4 Tone) Risk( AdminAltRow suspect ) => suspect switch
	{
		{ LinkedToBanned: true } => ("Ban evasion?", AdminMenuTheme.Danger),
		{ Score: >= 30 } => ("High", AdminMenuTheme.Warning),
		{ Score: >= 20 } => ("Medium", AdminMenuTheme.Warning),
		_ => ("Low", AdminMenuTheme.Neutral)
	};

	private static Vector4 EvidenceTone( string evidence ) => evidence switch
	{
		"Family share" => AdminMenuTheme.Danger,
		"Same IP" => AdminMenuTheme.Warning,
		_ => AdminMenuTheme.Neutral
	};
}
