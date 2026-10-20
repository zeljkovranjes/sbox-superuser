using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private bool _showCommandResult;
	private string _lastModelFeedback = "";

	private void DrawMenuBar()
	{
		if ( !ImGui.BeginMainMenuBar() ) return;

		if ( ImGui.BeginMenu( "Superuser" ) )
		{
			if ( ImGui.MenuItem( "Refresh" ) ) Model.Refresh();
			ImGui.SetItemTooltip( "{0}", "Views also refresh every 5 seconds." );
			WindowItem( "Documentation", DocumentationWindow );
			WindowItem( "About Superuser", AboutWindow );
			ImGui.Separator();
			if ( ImGui.MenuItem( "Close", "Esc" ) ) CloseRequested = true;
			ImGui.EndMenu();
		}

		// Moderation is about players; Server is about the server itself. Open windows are checked.
		if ( ImGui.BeginMenu( "Moderation" ) )
		{
			WindowItem( "Players", PlayersWindow );
			WindowItem( "Bans & mutes", RestrictionsWindow );
			WindowItem( "Chat", ChatWindow );
			ImGui.EndMenu();
		}

		if ( ImGui.BeginMenu( "Server" ) )
		{
			WindowItem( "Commands", CommandsWindow );
			WindowItem( "Activity", ActivityWindow );
			if ( CanEditPermissions ) WindowItem( "Permissions", PermissionsWindow );
			if ( Server.Can( PrefixExportCommand ) ) WindowItem( "Prefixes", PrefixesWindow );
			ImGui.EndMenu();
		}

		DrawLastResult();

		ImGui.EndMainMenuBar();
	}

	/// <summary>The newest outcome on the right of the bar: a moderation action or a command run from any window.</summary>
	private void DrawLastResult()
	{
		if ( Model.Feedback != _lastModelFeedback )
		{
			_lastModelFeedback = Model.Feedback;
			_showCommandResult = false;
			// Only finished outcomes pop up; "Waiting for the host..." stays on the bar.
			if ( Model.FeedbackStatus is { } finished ) Notify( Model.Feedback, AdminMenuTheme.Tone( finished ) );
		}

		string message;
		Vector4 color;
		if ( _showCommandResult && Server.LastResult is { } result )
		{
			message = $"{Server.LastCommand}: {result.Status}. {result.Message}";
			color = AdminMenuTheme.Tone( result.Status );
		}
		else if ( Model.Feedback.Length > 0 )
		{
			// Feedback already reads "Waiting for the host..." while an action is pending.
			message = Model.Feedback;
			color = Model.FeedbackStatus is { } status ? AdminMenuTheme.Tone( status ) : AdminMenuTheme.Warning;
		}
		else
		{
			return;
		}

		ImGui.Separator();
		ImGui.TextColored( color, "{0}", message );
		ImGui.SetItemTooltip( "{0}", message );
	}

	/// <summary>Toggles a window; the checkmark shows it is open and "pinned" that it stays while the menu is closed.</summary>
	private void WindowItem( string label, string window )
	{
		var layout = _layout.Window( window );
		if ( ImGui.MenuItem( label, layout.Pinned ? "pinned" : null, layout.Open ) ) _layout.SetOpen( window, !layout.Open );
	}
}
