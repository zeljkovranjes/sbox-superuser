using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private static readonly CommandStatus[] Outcomes = Enum.GetValues<CommandStatus>();

	// Filter text being typed; it reaches the model only when Apply validates it.
	private string _activityCommandInput = "";
	private string _activityActorInput = "";
	private CommandStatus? _activityOutcomeInput;
	private string _activityFilterError = "";

	private void DrawActivityWindow()
	{
		if ( !BeginWindow( ActivityWindow, "Activity", new Vector2( 56, 640 ), new Vector2( 820, 380 ), new Vector2( 460, 220 ), out var expanded ) ) return;
		if ( expanded ) DrawActivity();
		ImGui.End();
	}

	private void DrawActivity()
	{
		Caption( "Audit records visible to you, newest first. Unknown outcomes are never success; inspect them before retrying." );
		DrawActivityFilters();
		var activity = Model.Activity;
		if ( activity is null ) { Caption( "Loading activity..." ); return; }
		if ( activity.Status != CommandStatus.Success ) ColoredWrapped( activity.Message, AdminMenuTheme.Tone( activity.Status ) );
		if ( activity.Activity.Length == 0 )
		{
			if ( activity.Status == CommandStatus.Success ) Caption( "No activity matches." );
			return;
		}

		var pager = activity.HasMore || Model.ActivityPage > 0;
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY;
		if ( ImGui.BeginTable( "activity-table", 5, flags, new Vector2( 0, pager ? -ImGui.GetFrameHeightWithSpacing() : 0 ) ) )
		{
			ImGui.TableSetupScrollFreeze( 0, 1 );
			ImGui.TableSetupColumn( "Time (UTC)", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "0000-00-00 00:00" ).x );
			ImGui.TableSetupColumn( "Actor", ImGuiTableColumnFlags.WidthStretch, 1.4f );
			ImGui.TableSetupColumn( "Command", ImGuiTableColumnFlags.WidthStretch, 1 );
			ImGui.TableSetupColumn( "Outcome", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "StorageUnavailable" ).x );
			ImGui.TableSetupColumn( "Request", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "00000000..." ).x );
			ImGui.TableHeadersRow();
			foreach ( var row in activity.Activity ) DrawActivityRow( row );
			ImGui.EndTable();
		}

		if ( !pager ) return;
		var change = Pager( "activity", Model.ActivityPage, activity.HasMore, "Newer", "Older" );
		if ( change == 0 ) return;
		Model.ActivityPage += change;
		Model.RefreshActivity();
	}

	private void DrawActivityRow( AdminActivityRow row )
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.TextUnformatted( Utc( row.CreatedUtc ) );
		ImGui.TableNextColumn();
		ImGui.TextWrapped( "{0}", NameOf( row.Actor ) is { } name ? $"{name}  ({row.Actor})" : row.Actor );
		ImGui.TableNextColumn();
		ImGui.TextWrapped( "{0}", row.Command );
		ImGui.TableNextColumn();
		ImGui.TextColored( AdminMenuTheme.Tone( row.Outcome ), "{0}", row.Outcome.ToString() );
		ImGui.TableNextColumn();
		var request = row.RequestId.ToString( "N" );
		ImGui.TextDisabled( "{0}...", request.Substring( 0, 8 ) );
		ImGui.SetItemTooltip( "Request: {0}", request );
	}

	private void DrawActivityFilters()
	{
		var style = ImGui.GetStyle();
		var buttons = ButtonWidth( "Apply" ) + ButtonWidth( "Clear" ) + style.ItemSpacing.x * 2;
		var field = MathF.Max( 80 * Scale, (ImGui.GetContentRegionAvail().x - buttons - 140 * Scale - style.ItemSpacing.x * 2) / 2 );
		var submit = Field( "##activity-command", "Command, e.g. ban", ref _activityCommandInput, 64, field, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		submit |= Field( "##activity-actor", "Actor Steam ID", ref _activityActorInput, 17, field, ImGuiInputTextFlags.EnterReturnsTrue );
		ImGui.SameLine();
		ImGui.SetNextItemWidth( 140 * Scale );
		if ( ImGui.BeginCombo( "##activity-outcome", _activityOutcomeInput?.ToString() ?? "Any outcome" ) )
		{
			if ( ImGui.Selectable( "Any outcome", _activityOutcomeInput is null ) ) _activityOutcomeInput = null;
			foreach ( var outcome in Outcomes )
				if ( ImGui.Selectable( outcome.ToString(), _activityOutcomeInput == outcome ) ) _activityOutcomeInput = outcome;
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		if ( ImGui.Button( "Apply" ) || submit ) ApplyActivityFilters();
		ImGui.SameLine();
		if ( ImGui.Button( "Clear" ) )
		{
			_activityCommandInput = "";
			_activityActorInput = "";
			_activityOutcomeInput = null;
			ApplyActivityFilters();
		}
		if ( _activityFilterError.Length > 0 ) ImGui.TextColored( AdminMenuTheme.Warning, "{0}", _activityFilterError );
	}

	private void ApplyActivityFilters()
	{
		var command = _activityCommandInput.Trim().ToLowerInvariant();
		var actor = _activityActorInput.Trim();
		// Building the query applies the host's own rules, so nothing invalid is ever sent.
		try { _ = new AdminQuery( command, actor, _activityOutcomeInput ); }
		catch ( ArgumentException )
		{
			_activityFilterError = "Use a command key such as ban or group.create, and a full Steam ID for the actor.";
			return;
		}
		_activityFilterError = "";
		Model.ActivityCommand = command;
		Model.ActivityActor = actor;
		Model.ActivityOutcome = _activityOutcomeInput;
		Model.ActivityPage = 0;
		Model.RefreshActivity();
	}
}
