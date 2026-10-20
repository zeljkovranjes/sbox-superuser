using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	// Every module reads the same way: Overview, its own Guides, then generated Commands and Permissions.
	private const int OverviewSection = -1;
	private const int CommandsSection = -2;
	private const int PermissionsSection = -3;

	private string _docModule = "superuser.administration";
	private int _docSection = OverviewSection;

	private void DrawDocumentationWindow()
	{
		if ( !BeginWindow( DocumentationWindow, "Documentation", new Vector2( 120, 60 ), new Vector2( 940, 620 ), new Vector2( 560, 320 ), out var expanded ) ) return;
		if ( expanded )
		{
			Server.RequestDocumentation();
			var reply = Server.Documentation;
			if ( reply is null ) Caption( "Loading documentation..." );
			else if ( reply.Status != CommandStatus.Success ) ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) );
			else DrawDocumentationPanes( reply.Modules );
		}
		ImGui.End();
	}

	private void DrawDocumentationPanes( AdminModuleRow[] modules )
	{
		var available = ImGui.GetContentRegionAvail();
		ImGui.BeginChild( "doc-tree", new Vector2( MathF.Max( 220 * Scale, available.x * 0.26f ), 0 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX );
		if ( ImGui.SmallButton( "Refresh" ) ) Server.RefreshDocumentation();
		ImGui.Separator();
		foreach ( var module in modules ) DrawDocumentationTree( module );
		ImGui.EndChild();
		ImGui.SameLine();

		ImGui.BeginChild( "doc-page", Vector2.Zero, ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding );
		var selected = modules.FirstOrDefault( module => module.Id == _docModule ) ?? modules.FirstOrDefault();
		if ( selected is null ) Caption( "No documented modules." );
		else DrawDocumentationPage( selected );
		ImGui.EndChild();
	}

	private void DrawDocumentationTree( AdminModuleRow module )
	{
		// Module IDs are validated keys, so they are safe as tree labels; page titles are drawn as text.
		ImGui.SetNextItemOpen( module.Id == _docModule, ImGuiCond.Appearing );
		var open = ImGui.TreeNode( module.Id );
		if ( module.Version.Length > 0 )
		{
			ImGui.SameLine();
			ImGui.TextDisabled( "{0}", module.Version );
		}
		if ( !open ) return;
		DocumentationLink( module.Id, OverviewSection, "Overview" );
		for ( var page = 0; page < module.Pages.Length; page++ ) DocumentationLink( module.Id, page, module.Pages[ page ] );
		DocumentationLink( module.Id, CommandsSection, "Commands" );
		DocumentationLink( module.Id, PermissionsSection, "Permissions" );
		ImGui.TreePop();
	}

	private void DocumentationLink( string module, int section, string title )
	{
		ImGui.PushID( section );
		var selected = _docModule == module && _docSection == section;
		if ( ImGui.Selectable( "##link", selected, ImGuiSelectableFlags.AllowOverlap ) )
		{
			_docModule = module;
			_docSection = section;
		}
		ImGui.SameLine();
		if ( section >= 0 ) ImGui.TextUnformatted( title );
		else ImGui.TextDisabled( "{0}", title );
		ImGui.PopID();
	}

	private void DrawDocumentationPage( AdminModuleRow module )
	{
		switch ( _docSection )
		{
			case OverviewSection: DrawModuleOverview( module ); break;
			case CommandsSection: DrawModuleCommands( module ); break;
			case PermissionsSection: DrawModulePermissions( module ); break;
			default:
				if ( _docSection >= module.Pages.Length ) { _docSection = OverviewSection; break; }
				var page = Server.DocumentationPage( module.Id, _docSection );
				if ( page is null ) Caption( "Loading page..." );
				else if ( page.Status != CommandStatus.Success ) ColoredWrapped( page.Message, AdminMenuTheme.Tone( page.Status ) );
				else DrawMarkdown( page.Document );
				break;
		}
	}

	private void DrawModuleOverview( AdminModuleRow module )
	{
		ImGui.PushFont( null, 22 );
		ImGui.TextUnformatted( module.Id );
		ImGui.PopFont();
		if ( module.Description.Length > 0 ) ImGui.TextWrapped( "{0}", module.Description );
		ImGui.Separator();

		if ( ImGui.BeginTable( "overview", 2, ImGuiTableFlags.SizingFixedFit ) )
		{
			OverviewRow( "Version", module.Version.Length > 0 ? module.Version : "not set" );
			OverviewRow( "Author", module.Author.Length > 0 ? module.Author : "not set" );
			if ( module.Website.Length > 0 ) OverviewRow( "Website", module.Website );
			OverviewRow( "State", module.State );
			OverviewRow( "Module API", module.ApiVersion.ToString() );
			OverviewRow( "Depends on", module.Dependencies.Length > 0 ? string.Join( ", ", module.Dependencies ) : "nothing" );
			ImGui.EndTable();
		}
		if ( module.Website.Length > 0 && ImGui.SmallButton( "Copy website" ) ) Sandbox.UI.Clipboard.SetText( module.Website );
		if ( module.Message.Length > 0 ) Caption( module.Message );

		ImGui.SeparatorText( "Guides" );
		if ( module.Pages.Length == 0 ) Caption( "This module has no written guides yet; its commands and permissions are listed below." );
		for ( var page = 0; page < module.Pages.Length; page++ )
		{
			ImGui.PushID( page );
			ImGui.Bullet();
			ImGui.SameLine();
			if ( ImGui.SmallButton( "Open" ) ) _docSection = page;
			ImGui.SameLine();
			ImGui.TextUnformatted( module.Pages[ page ] );
			ImGui.PopID();
		}
	}

	private static void OverviewRow( string key, string value )
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.TextDisabled( "{0}", key );
		ImGui.TableNextColumn();
		ImGui.TextUnformatted( value );
	}

	private void DrawModuleCommands( AdminModuleRow module )
	{
		ImGui.PushFont( null, 18 );
		ImGui.TextUnformatted( "Commands" );
		ImGui.PopFont();
		ImGui.Separator();
		var commands = Server.Commands?.Commands.Where( command => command.ModuleId == module.Id ).ToArray() ?? Array.Empty<AdminCommandRow>();
		if ( commands.Length == 0 ) { Caption( "No commands you can run come from this module." ); return; }
		Caption( "Run these from the console with the admin prefix, from chat with /admin, or from the Commands window." );

		foreach ( var command in commands )
		{
			ImGui.PushID( command.Name );
			ImGui.Spacing();
			ImGui.TextColored( CodeColor, "{0}", Syntax( command ) );
			ImGui.SameLine();
			if ( ImGui.SmallButton( "Open" ) )
			{
				_selectedCommand = command.Name;
				_layout.SetOpen( CommandsWindow, true );
			}
			ImGui.TextWrapped( "{0}", command.Description );
			foreach ( var argument in command.Arguments.Where( argument => argument.Description.Length > 0 ) )
			{
				ImGui.Bullet();
				ImGui.SameLine();
				ImGui.TextDisabled( "{0}", argument.Name );
				ImGui.SameLine();
				ImGui.TextWrapped( "{0}", argument.Description );
			}
			ImGui.PopID();
		}
	}

	// Required arguments in angle brackets, optional ones in square brackets; a trailing ... takes the rest of the line.
	private static string Syntax( AdminCommandRow command ) => command.Name + string.Concat( command.Arguments.Select( argument =>
		" " + (argument.Optional ? "[" : "<") + argument.Name + (argument.Remainder ? "..." : "") + (argument.Optional ? "]" : ">") ) );

	private static void DrawModulePermissions( AdminModuleRow module )
	{
		ImGui.PushFont( null, 18 );
		ImGui.TextUnformatted( "Permissions" );
		ImGui.PopFont();
		ImGui.Separator();
		if ( module.Permissions.Length == 0 ) { Caption( "This module registers no permissions." ); return; }
		Caption( "Grant these to groups or players in Server › Permissions, or with admin group <group> permission allow <key>." );
		if ( !ImGui.BeginTable( "permissions", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp ) ) return;
		ImGui.TableSetupColumn( "Permission", ImGuiTableColumnFlags.WidthFixed, 260 );
		ImGui.TableSetupColumn( "What it allows", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableHeadersRow();
		foreach ( var permission in module.Permissions )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextColored( CodeColor, "{0}", permission.Key );
			ImGui.TableNextColumn();
			if ( permission.Description.Length > 0 ) ImGui.TextWrapped( "{0}", permission.Description );
			else ImGui.TextDisabled( "{0}", "No description." );
		}
		ImGui.EndTable();
	}
}
