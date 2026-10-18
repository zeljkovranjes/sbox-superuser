using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string ProductVersion = "0.1.0-rc.1";
	private const int ModuleApiVersion = 1;
	private const string Author = "chomnr";
	private const string Package = "notpointless.chomnr_sbox_superuser";
	private const string SourceUrl = "https://github.com/zeljkovranjes/sbox-superuser";

	private void DrawAboutWindow()
	{
		if ( !BeginWindow( AboutWindow, "About Superuser", new Vector2( 480, 120 ), new Vector2( 520, 420 ), new Vector2( 360, 240 ), out var expanded ) ) return;
		if ( expanded ) DrawAbout();
		ImGui.End();
	}

	private void DrawAbout()
	{
		Model.RequestAbout();
		ImGui.Text( "Superuser {0}", ProductVersion );
		Caption( "In-process administration for s&box dedicated servers: commands, permissions, moderation and audit." );
		ImGui.Separator();

		if ( ImGui.BeginTable( "about-facts", 2, ImGuiTableFlags.SizingFixedFit ) )
		{
			AboutRow( "Created by", Author );
			AboutRow( "Package", Package );
			AboutRow( "Module API", ModuleApiVersion.ToString() );
			AboutRow( "Source", SourceUrl );
			ImGui.EndTable();
		}
		if ( ImGui.SmallButton( "Copy source link" ) ) Sandbox.UI.Clipboard.SetText( SourceUrl );

		if ( ImGui.CollapsingHeader( "Installed modules", ImGuiTreeNodeFlags.DefaultOpen ) ) DrawModules();
		if ( Model.About is { ModulesAllowed: true } owner && ImGui.CollapsingHeader( "Server status" ) ) DrawServerStatus( owner );

		ImGui.Separator();
		ImGui.TextDisabled( "{0}", "\"ban those fuckers\" - chomnr" );
	}

	private static void AboutRow( string key, string value )
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.TextDisabled( "{0}", key );
		ImGui.TableNextColumn();
		ImGui.TextUnformatted( value );
	}

	/// <summary>Host health for owners, the same fields as "admin diagnostics".</summary>
	private static void DrawServerStatus( AdminReply about )
	{
		if ( about.Data.Count == 0 ) { Caption( "No status reported." ); return; }
		if ( !ImGui.BeginTable( "server-status", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders ) ) return;
		ImGui.TableSetupColumn( "Field", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "retentionPending" ).x );
		ImGui.TableSetupColumn( "Value", ImGuiTableColumnFlags.WidthStretch );
		foreach ( var (field, value) in about.Data.OrderBy( pair => pair.Key, StringComparer.Ordinal ) )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextDisabled( "{0}", field );
			ImGui.TableNextColumn();
			ImGui.TextColored( StatusTone( value ), "{0}", value );
		}
		ImGui.EndTable();
	}

	// Healthy values read plainly; anything degraded or unavailable stands out.
	private static Vector4 StatusTone( string value ) => value switch
	{
		"true" or "Degraded" or "Unavailable" or "Unknown" or "Corrupt" => AdminMenuTheme.Warning,
		_ => ImGui.GetStyle()[ ImGuiCol.Text ]
	};

	private void DrawModules()
	{
		var about = Model.About;
		if ( about is null ) { Caption( "Loading..." ); return; }
		if ( about.Status != CommandStatus.Success ) { ColoredWrapped( about.Message, AdminMenuTheme.Tone( about.Status ) ); return; }
		if ( !about.ModulesAllowed ) { Caption( "Only owners can see the modules installed on this server." ); return; }
		if ( about.Modules.Length == 0 ) { Caption( "No modules are installed." ); return; }

		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable;
		if ( !ImGui.BeginTable( "modules-table", 4, flags ) ) return;
		ImGui.TableSetupColumn( "Module", ImGuiTableColumnFlags.WidthStretch, 1 );
		ImGui.TableSetupColumn( "API", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "API" ).x );
		ImGui.TableSetupColumn( "State", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Stopping" ).x );
		ImGui.TableSetupColumn( "Status", ImGuiTableColumnFlags.WidthStretch, 1.4f );
		ImGui.TableHeadersRow();
		foreach ( var module in about.Modules )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( module.Id );
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( module.ApiVersion.ToString() );
			ImGui.TableNextColumn();
			ImGui.TextColored( module.State == "Enabled" ? AdminMenuTheme.Success : AdminMenuTheme.Warning, "{0}", module.State );
			ImGui.TableNextColumn();
			ImGui.TextWrapped( "{0}", module.Message );
		}
		ImGui.EndTable();
	}
}
