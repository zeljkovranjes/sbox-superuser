using Sandbox;
using SboxSuperuser.Core.Menu;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	// Window titles and labels come from modules, so they never reach ImGui as IDs or format strings unescaped.
	private static string Label( string text ) => text.Replace( "#", "＃" );

	private static string WindowKey( AdminMenuWindowRow window ) => MenuLayout.ModuleWindowName( window.ModuleId, window.Title );

	/// <summary>The Modules menu: one entry per window the viewer's modules added, shown only when there are any.</summary>
	private void DrawModulesMenu()
	{
		var windows = Server.MenuWindows?.MenuWindows;
		if ( windows is not { Length: > 0 } || !ImGui.BeginMenu( "Modules" ) ) return;
		foreach ( var module in windows.GroupBy( window => window.ModuleId ) )
		{
			ImGui.SeparatorText( module.Key );
			foreach ( var window in module ) WindowItem( Label( window.Title ) + "##" + WindowKey( window ), WindowKey( window ) );
		}
		ImGui.EndMenu();
	}

	private void DrawModuleWindows()
	{
		foreach ( var window in Server.MenuWindows?.MenuWindows ?? Array.Empty<AdminMenuWindowRow>() ) DrawModuleWindow( window );
	}

	private void DrawModuleWindow( AdminMenuWindowRow window )
	{
		var key = WindowKey( window );
		if ( !BeginWindow( key, Label( window.Title ) + "###" + key, new Vector2( 220, 90 ), new Vector2( 560, 520 ), new Vector2( 340, 220 ), out var expanded ) ) return;
		if ( expanded )
		{
			ImGui.PushID( key );
			for ( var index = 0; index < window.Elements.Length; index++ )
			{
				ImGui.PushID( index );
				DrawModuleElement( window.Elements[ index ], $"{key}/{index}" );
				ImGui.PopID();
			}
			ImGui.PopID();
		}
		ImGui.End();
	}

	private void DrawModuleElement( AdminMenuElementRow element, string key )
	{
		switch ( element.Kind )
		{
			case AdminMenuElementKind.Heading:
				ImGui.SeparatorText( Label( element.Text ) );
				break;
			case AdminMenuElementKind.Text:
				ImGui.TextWrapped( "{0}", element.Text );
				break;
			case AdminMenuElementKind.Separator:
				ImGui.Separator();
				break;
			case AdminMenuElementKind.Button:
				DrawModuleButton( element );
				break;
			case AdminMenuElementKind.Form:
				if ( FindCommand( element.Command ) is { } command )
				{
					ImGui.BeginChild( "form", Vector2.Zero, ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding );
					DrawCommandForm( command );
					ImGui.EndChild();
				}
				break;
			case AdminMenuElementKind.Live:
				DrawModuleLive( element, key );
				break;
		}
	}

	private void DrawModuleButton( AdminMenuElementRow element )
	{
		var command = FindCommand( element.Command );
		ImGui.BeginDisabled( command is null || Server.Running );
		if ( ImGui.Button( Label( element.Text ) ) && command is not null )
		{
			var details = command.Arguments.Zip( element.Arguments ).Select( pair => (pair.First.Name, pair.Second) ).ToArray();
			RunCommand( $"{element.Text}?", command.Name, element.Arguments, confirm: command.Mutation, details: details );
		}
		ImGui.EndDisabled();
		if ( command is not null ) ImGui.SetItemTooltip( "{0}", command.Description );
	}

	// A live element shows a read-only command's reply: its message, then its rows, then any other fields.
	private void DrawModuleLive( AdminMenuElementRow element, string key )
	{
		var reply = Server.Live( key, element.Command, element.Arguments, DateTimeOffset.UtcNow );
		if ( reply is null )
		{
			Caption( "Loading..." );
			return;
		}
		if ( reply.Status != CommandStatus.Success )
		{
			ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) );
		}
		else
		{
			ImGui.TextWrapped( "{0}", reply.Message );
			var rows = reply.Data.Where( pair => pair.Key.StartsWith( "row", StringComparison.Ordinal ) )
				.OrderBy( pair => int.TryParse( pair.Key.AsSpan( 3 ), out var number ) ? number : int.MaxValue ).ToArray();
			foreach ( var row in rows )
			{
				ImGui.Bullet();
				ImGui.SameLine();
				ImGui.TextWrapped( "{0}", row.Value );
			}
			foreach ( var field in reply.Data.Where( pair => !pair.Key.StartsWith( "row", StringComparison.Ordinal ) && pair.Key is not ("more" or "page") ) )
				ImGui.TextDisabled( "{0}: {1}", field.Key, field.Value );
		}
		if ( ImGui.SmallButton( "Refresh" ) ) Server.RefreshLive();
	}

	private AdminCommandRow FindCommand( string name ) => Server.Commands?.Commands.FirstOrDefault( command => command.Name == name );
}
