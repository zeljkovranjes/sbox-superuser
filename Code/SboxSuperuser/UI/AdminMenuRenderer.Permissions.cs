using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	// The owner-only policy commands; the window appears only for callers who may run them.
	private const string GroupAllowCommand = "group.permission.allow";
	private const string ExplainCommand = "permissions.explain";

	private string _selectedGroup = "";
	private string _selectedUser = "";
	private string _newGroup = "";
	private string _newMember = "";
	private string _newParents = "";
	private string _newProtection = "";
	private string _permissionToAdd = "";
	private string _explainPlayer = "";
	private string _explainPermission = "";

	private bool CanEditPermissions => Server.Can( GroupAllowCommand );

	/// <summary>Groups, players and an access check; edits run the audited policy commands.</summary>
	private void DrawPermissionsWindow()
	{
		if ( !CanEditPermissions ) return;
		if ( !BeginWindow( PermissionsWindow, "Permissions", new Vector2( 136, 136 ), new Vector2( 900, 560 ), new Vector2( 560, 320 ), out var expanded ) ) return;
		if ( expanded )
		{
			Caption( "Groups, their permissions and members. Every change is confirmed, versioned and audited." );
			var reply = Server.Permissions;
			if ( reply is null ) Caption( "Loading permissions..." );
			else if ( reply.Status != CommandStatus.Success ) ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) );
			else if ( ImGui.BeginTabBar( "permission-tabs" ) )
			{
				if ( ImGui.BeginTabItem( "Groups" ) ) { DrawGroupsTab( reply ); ImGui.EndTabItem(); }
				if ( ImGui.BeginTabItem( "Players" ) ) { DrawUsersTab( reply ); ImGui.EndTabItem(); }
				if ( ImGui.BeginTabItem( "Check access" ) ) { DrawExplainTab( reply ); ImGui.EndTabItem(); }
				ImGui.EndTabBar();
			}
		}
		ImGui.End();
	}

	private void DrawGroupsTab( AdminReply reply )
	{
		var available = ImGui.GetContentRegionAvail();
		ImGui.BeginChild( "group-list", new Vector2( MathF.Max( 200 * Scale, available.x * 0.28f ), 0 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX );
		foreach ( var group in reply.Groups )
		{
			// Group IDs are validated keys, so they are safe as labels.
			if ( ImGui.Selectable( group.Id, group.Id == _selectedGroup ) ) SelectGroup( group );
			ImGui.SetItemTooltip( "{0} members, protection {1}", group.Members.Length, group.Protection );
		}
		ImGui.Separator();
		var submit = Field( "##new-group", "New group ID", ref _newGroup, 64, ImGui.GetContentRegionAvail().x, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.BeginDisabled( _newGroup.Trim().Length == 0 );
		if ( ImGui.Button( "Create group", new Vector2( -1, 0 ) ) || submit && _newGroup.Trim().Length > 0 )
		{
			RunCommand( "Create this group?", "group.create", new[] { _newGroup.Trim() }, confirm: true, details: ("Group", _newGroup.Trim()) );
			_newGroup = "";
		}
		ImGui.EndDisabled();
		ImGui.EndChild();
		ImGui.SameLine();

		ImGui.BeginChild( "group-detail", Vector2.Zero, ImGuiChildFlags.Borders );
		var selected = reply.Groups.FirstOrDefault( group => group.Id == _selectedGroup );
		if ( selected is null ) Caption( "Pick a group on the left." );
		else DrawGroupDetail( selected, reply.PermissionKeys );
		ImGui.EndChild();
	}

	private void SelectGroup( AdminGroupRow group )
	{
		_selectedGroup = group.Id;
		_newParents = string.Join( ",", group.Parents );
		_newProtection = group.Protection.ToString();
	}

	private void DrawGroupDetail( AdminGroupRow group, string[] permissionKeys )
	{
		ImGui.TextUnformatted( group.Id );
		ImGui.SameLine();
		ImGui.TextDisabled( "{0} members", group.Members.Length );

		ImGui.SeparatorText( "Settings" );
		var narrow = 200 * Scale;
		Field( "##parents", "Comma-separated parent groups", ref _newParents, 256, narrow * 1.6f );
		ImGui.SameLine();
		if ( ImGui.Button( "Set parents" ) )
			RunCommand( "Change this group's parents?", "group.parents.set", new[] { group.Id, _newParents.Trim() }, confirm: true,
				details: new[] { ("Group", group.Id), ("Parents", _newParents.Trim().Length == 0 ? "(none)" : _newParents.Trim()) } );
		Field( "##protection", "0–99", ref _newProtection, 2, narrow * 0.5f );
		ImGui.SameLine();
		if ( ImGui.Button( "Set protection" ) )
			RunCommand( "Change this group's protection level?", "group.protection.set", new[] { group.Id, _newProtection.Trim() }, confirm: true,
				details: new[] { ("Group", group.Id), ("Protection", _newProtection.Trim()) } );
		ImGui.SetItemTooltip( "{0}", "Players can only punish targets whose protection is lower than their own." );

		ImGui.SeparatorText( "Permissions" );
		DrawEntryTable( "group-entries", group.Allowed, group.Denied, key =>
			RunCommand( "Remove this permission entry?", "group.permission.unset", new[] { group.Id, key }, confirm: true,
				details: new[] { ("Group", group.Id), ("Permission", key) } ) );
		DrawPermissionAdder( permissionKeys, (key, allow) =>
			RunCommand( allow ? "Allow this permission?" : "Deny this permission?", allow ? GroupAllowCommand : "group.permission.deny",
				new[] { group.Id, key }, confirm: true, danger: !allow, details: new[] { ("Group", group.Id), ("Permission", key) } ) );

		ImGui.SeparatorText( "Members" );
		foreach ( var member in group.Members )
		{
			ImGui.PushID( member );
			if ( ImGui.SmallButton( "Remove" ) )
				RunCommand( "Remove this player from the group?", "user.group.remove", new[] { member, group.Id }, confirm: true,
					details: new[] { ("Player", Describe( member )), ("Group", group.Id) } );
			ImGui.SameLine();
			ImGui.TextUnformatted( Describe( member ) );
			ImGui.PopID();
		}
		if ( group.Members.Length == 0 ) Caption( "No members." );
		Field( "##new-member", "Steam ID to add", ref _newMember, 17, narrow * 1.6f );
		ImGui.SameLine();
		ImGui.BeginDisabled( _newMember.Trim().Length == 0 );
		if ( ImGui.Button( "Add member" ) )
		{
			RunCommand( "Add this player to the group?", "user.group.add", new[] { _newMember.Trim(), group.Id }, confirm: true,
				details: new[] { ("Player", Describe( _newMember.Trim() )), ("Group", group.Id) } );
			_newMember = "";
		}
		ImGui.EndDisabled();

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.PushStyleColor( ImGuiCol.Text, AdminMenuTheme.Danger );
		if ( ImGui.Button( "Delete group" ) )
			RunCommand( "Delete this group?", "group.delete", new[] { group.Id }, confirm: true, danger: true,
				details: new[] { ("Group", group.Id), ("Members", group.Members.Length.ToString()) } );
		ImGui.PopStyleColor();
	}

	private void DrawUsersTab( AdminReply reply )
	{
		var available = ImGui.GetContentRegionAvail();
		ImGui.BeginChild( "user-list", new Vector2( MathF.Max( 240 * Scale, available.x * 0.34f ), 0 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX );
		foreach ( var user in reply.Users )
		{
			ImGui.PushID( user.SteamId );
			if ( ImGui.Selectable( "##user", user.SteamId == _selectedUser, ImGuiSelectableFlags.AllowOverlap ) ) _selectedUser = user.SteamId;
			ImGui.SameLine();
			ImGui.TextUnformatted( Describe( user.SteamId ) );
			ImGui.PopID();
		}
		if ( reply.Users.Length == 0 ) Caption( "No players have explicit groups or permissions yet." );
		ImGui.EndChild();
		ImGui.SameLine();

		ImGui.BeginChild( "user-detail", Vector2.Zero, ImGuiChildFlags.Borders );
		var selected = reply.Users.FirstOrDefault( user => user.SteamId == _selectedUser );
		if ( selected is null ) Caption( "Pick a player on the left. Add players to groups from the Groups tab." );
		else DrawUserDetail( selected, reply.PermissionKeys );
		ImGui.EndChild();
	}

	private void DrawUserDetail( AdminUserRow user, string[] permissionKeys )
	{
		ImGui.TextUnformatted( Describe( user.SteamId ) );
		ImGui.SeparatorText( "Groups" );
		foreach ( var group in user.Groups )
		{
			ImGui.PushID( group );
			if ( ImGui.SmallButton( "Remove" ) )
				RunCommand( "Remove this player from the group?", "user.group.remove", new[] { user.SteamId, group }, confirm: true,
					details: new[] { ("Player", Describe( user.SteamId )), ("Group", group) } );
			ImGui.SameLine();
			ImGui.TextUnformatted( group );
			ImGui.PopID();
		}
		if ( user.Groups.Length == 0 ) Caption( "No groups." );

		ImGui.SeparatorText( "Direct permissions" );
		DrawEntryTable( "user-entries", user.Allowed, user.Denied, key =>
			RunCommand( "Remove this permission entry?", "user.permission.unset", new[] { user.SteamId, key }, confirm: true,
				details: new[] { ("Player", Describe( user.SteamId )), ("Permission", key) } ) );
		DrawPermissionAdder( permissionKeys, (key, allow) =>
			RunCommand( allow ? "Allow this permission?" : "Deny this permission?", allow ? "user.permission.allow" : "user.permission.deny",
				new[] { user.SteamId, key }, confirm: true, danger: !allow, details: new[] { ("Player", Describe( user.SteamId )), ("Permission", key) } ) );
	}

	private void DrawExplainTab( AdminReply reply )
	{
		Caption( "Shows whether a player has a permission, and which group or entry decides it." );
		var width = 260 * Scale;
		Field( "##explain-player", "Steam ID", ref _explainPlayer, 17, width );
		ImGui.SameLine();
		DrawOnlinePlayerPicker( ref _explainPlayer );
		DrawPermissionCombo( "##explain-permission", reply.PermissionKeys, ref _explainPermission, width );
		ImGui.BeginDisabled( _explainPlayer.Trim().Length == 0 || _explainPermission.Length == 0 || Server.Running );
		if ( ImGui.Button( "Check" ) ) RunCommand( "", ExplainCommand, new[] { _explainPlayer.Trim(), _explainPermission }, confirm: false );
		ImGui.EndDisabled();
		if ( Server.LastCommand == ExplainCommand && Server.LastResult is { } result )
		{
			ImGui.SeparatorText( "Result" );
			DrawCommandResult( result );
		}
	}

	/// <summary>Allowed and denied entries with a remove button each.</summary>
	private void DrawEntryTable( string id, string[] allowed, string[] denied, Action<string> remove )
	{
		if ( allowed.Length + denied.Length == 0 ) { Caption( "No direct entries." ); return; }
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders;
		if ( !ImGui.BeginTable( id, 3, flags ) ) return;
		ImGui.TableSetupColumn( "Permission", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableSetupColumn( "Effect", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Allow" ).x );
		ImGui.TableSetupColumn( "##remove", ImGuiTableColumnFlags.WidthFixed, ButtonWidth( "Remove" ) );
		ImGui.TableHeadersRow();
		foreach ( var (key, allow) in allowed.Select( key => (key, true) ).Concat( denied.Select( key => (key, false) ) ) )
		{
			ImGui.TableNextRow();
			ImGui.PushID( key );
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( key );
			ImGui.TableNextColumn();
			ImGui.TextColored( allow ? AdminMenuTheme.Success : AdminMenuTheme.Danger, "{0}", allow ? "Allow" : "Deny" );
			ImGui.TableNextColumn();
			if ( ImGui.SmallButton( "Remove" ) ) remove( key );
			ImGui.PopID();
		}
		ImGui.EndTable();
	}

	private void DrawPermissionAdder( string[] permissionKeys, Action<string, bool> add )
	{
		DrawPermissionCombo( "##add-permission", permissionKeys, ref _permissionToAdd, 280 * Scale );
		ImGui.SameLine();
		ImGui.BeginDisabled( _permissionToAdd.Length == 0 );
		if ( ImGui.Button( "Allow" ) ) add( _permissionToAdd, true );
		ImGui.SameLine();
		if ( ImGui.Button( "Deny" ) ) add( _permissionToAdd, false );
		ImGui.EndDisabled();
	}

	private static void DrawPermissionCombo( string id, string[] keys, ref string value, float width )
	{
		ImGui.SetNextItemWidth( width );
		if ( !ImGui.BeginCombo( id, value.Length == 0 ? "Choose a permission..." : value, ImGuiComboFlags.HeightLarge ) ) return;
		// Permission keys are validated registry keys, so they are safe as labels.
		foreach ( var key in keys ) if ( ImGui.Selectable( key, key == value ) ) value = key;
		ImGui.EndCombo();
	}

	private string Describe( string steamId ) => NameOf( steamId ) is { } name ? $"{name}  ({steamId})" : steamId;
}
