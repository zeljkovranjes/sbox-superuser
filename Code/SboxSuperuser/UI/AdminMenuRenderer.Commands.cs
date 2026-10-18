using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private string _commandFilter = "";
	private string _selectedCommand = "";

	// Typed values per command and argument, kept while the menu is open so switching commands loses nothing.
	private readonly Dictionary<string, string> _commandValues = new();

	/// <summary>Every command the caller may run, with a form built from its arguments.</summary>
	private void DrawCommandsWindow()
	{
		if ( !BeginWindow( CommandsWindow, "Commands", new Vector2( 96, 96 ), new Vector2( 840, 520 ), new Vector2( 520, 300 ), out var expanded ) ) return;
		if ( expanded )
		{
			Caption( "Everything you can run on this server, including the game's own commands. Results are private to you." );
			var reply = Server.Commands;
			if ( reply is null ) Caption( "Loading commands..." );
			else if ( reply.Status != CommandStatus.Success ) ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) );
			else DrawCommandPanes( reply.Commands );
		}
		ImGui.End();
	}

	private void DrawCommandPanes( AdminCommandRow[] commands )
	{
		var available = ImGui.GetContentRegionAvail();
		ImGui.BeginChild( "command-list", new Vector2( MathF.Max( 220 * Scale, available.x * 0.32f ), 0 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX );
		DrawCommandList( commands );
		ImGui.EndChild();
		ImGui.SameLine();
		ImGui.BeginChild( "command-form", Vector2.Zero, ImGuiChildFlags.Borders );
		var selected = commands.FirstOrDefault( command => command.Name == _selectedCommand );
		if ( selected is null ) Caption( "Pick a command on the left." );
		else DrawCommandForm( selected );
		ImGui.EndChild();
	}

	private void DrawCommandList( AdminCommandRow[] commands )
	{
		Field( "##command-filter", "Filter", ref _commandFilter, 64, ImGui.GetContentRegionAvail().x );
		var filter = _commandFilter.Trim();
		// Superuser's own commands first, then each game module under its ID.
		foreach ( var module in commands.GroupBy( command => command.ModuleId ).OrderBy( group => IsBuiltIn( group.Key ) ? "" : group.Key ) )
		{
			var visible = module.Where( command => filter.Length == 0
				|| command.Name.Contains( filter, StringComparison.OrdinalIgnoreCase )
				|| command.Description.Contains( filter, StringComparison.OrdinalIgnoreCase ) ).ToArray();
			if ( visible.Length == 0 ) continue;
			ImGui.SeparatorText( IsBuiltIn( module.Key ) ? "Superuser" : module.Key );
			foreach ( var command in visible )
			{
				// Command keys are validated lowercase keys, so they are safe as ImGui labels.
				if ( ImGui.Selectable( command.Name, command.Name == _selectedCommand ) ) _selectedCommand = command.Name;
				ImGui.SetItemTooltip( "{0}", command.Description );
			}
		}
	}

	private static bool IsBuiltIn( string moduleId ) => moduleId.StartsWith( "superuser.", StringComparison.Ordinal );

	private void DrawCommandForm( AdminCommandRow command )
	{
		ImGui.TextUnformatted( command.Name );
		if ( command.Mutation )
		{
			ImGui.SameLine();
			ImGui.TextColored( AdminMenuTheme.Warning, "{0}", "changes state" );
		}
		Caption( command.Description );
		ImGui.Separator();

		ImGui.PushID( command.Name );
		foreach ( var argument in command.Arguments ) DrawArgumentInput( command, argument );
		var error = BuildArguments( command, out var values );
		ImGui.Spacing();
		if ( error is not null ) ImGui.TextColored( AdminMenuTheme.Warning, "{0}", error );
		ImGui.BeginDisabled( error is not null || Server.Running );
		if ( ImGui.Button( Server.Running && Server.LastCommand == command.Name ? "Running..." : "Run", new Vector2( 120 * Scale, 0 ) ) )
		{
			var details = command.Arguments.Zip( values ).Select( pair => (pair.First.Name, pair.Second) ).ToArray();
			RunCommand( $"Run {command.Name}?", command.Name, values, confirm: command.Mutation, details: details );
		}
		ImGui.EndDisabled();
		ImGui.PopID();

		if ( Server.LastCommand == command.Name && Server.LastResult is { } result )
		{
			ImGui.SeparatorText( "Result" );
			DrawCommandResult( result );
		}
	}

	private void DrawArgumentInput( AdminCommandRow command, AdminCommandArgumentRow argument )
	{
		var key = command.Name + "/" + argument.Name;
		var value = _commandValues.GetValueOrDefault( key, argument.Kind == CommandArgumentKind.Boolean ? "false" : "" );
		var label = argument.Optional ? $"{argument.Name} (optional)" : argument.Name;
		var width = ImGui.GetContentRegionAvail().x * 0.6f;
		ImGui.PushID( argument.Name );

		switch ( argument.Kind )
		{
			case CommandArgumentKind.Boolean:
				var enabled = value == "true";
				if ( ImGui.Checkbox( label, ref enabled ) ) value = enabled ? "true" : "false";
				break;
			case CommandArgumentKind.Enum:
				ImGui.SetNextItemWidth( width );
				if ( ImGui.BeginCombo( label, value.Length == 0 ? "Choose..." : value ) )
				{
					if ( argument.Optional && ImGui.Selectable( "(none)", value.Length == 0 ) ) value = "";
					foreach ( var choice in argument.Choices )
					{
						ImGui.PushID( choice );
						if ( ImGui.Selectable( "##choice", choice == value, ImGuiSelectableFlags.AllowOverlap ) ) value = choice;
						ImGui.SameLine();
						ImGui.TextUnformatted( choice );
						ImGui.PopID();
					}
					ImGui.EndCombo();
				}
				break;
			case CommandArgumentKind.Player:
				Field( "##value", "Steam ID or exact online name", ref value, 64, width - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemInnerSpacing.x );
				ImGui.SameLine( 0, ImGui.GetStyle().ItemInnerSpacing.x );
				DrawOnlinePlayerPicker( ref value );
				ImGui.SameLine();
				ImGui.TextUnformatted( label );
				break;
			case CommandArgumentKind.Duration:
				Field( "##value", "30m, 2h, 7d or permanent", ref value, 32, width - 120 * Scale - ImGui.GetStyle().ItemInnerSpacing.x );
				ImGui.SameLine( 0, ImGui.GetStyle().ItemInnerSpacing.x );
				ImGui.SetNextItemWidth( 120 * Scale );
				if ( ImGui.BeginCombo( "##presets", "Presets" ) )
				{
					foreach ( var preset in DurationPresets ) if ( ImGui.Selectable( preset, preset == value ) ) value = preset;
					ImGui.EndCombo();
				}
				ImGui.SameLine();
				ImGui.TextUnformatted( label );
				break;
			default:
				Field( "##value", Hint( argument ), ref value, 256, argument.Remainder ? ImGui.GetContentRegionAvail().x * 0.8f : width );
				ImGui.SameLine();
				ImGui.TextUnformatted( label );
				break;
		}
		if ( argument.Description.Length > 0 ) ImGui.SetItemTooltip( "{0}", argument.Description );
		ImGui.PopID();
		_commandValues[ key ] = value;
	}

	private void DrawOnlinePlayerPicker( ref string value )
	{
		var players = Model.Players?.Players ?? Array.Empty<AdminPlayerRow>();
		ImGui.BeginDisabled( players.Length == 0 );
		if ( ImGui.BeginCombo( "##online", "", ImGuiComboFlags.NoPreview ) )
		{
			foreach ( var player in players )
			{
				ImGui.PushID( player.SteamId );
				if ( ImGui.Selectable( "##pick", player.SteamId == value, ImGuiSelectableFlags.AllowOverlap ) ) value = player.SteamId;
				ImGui.SameLine();
				ImGui.TextUnformatted( player.Name );
				ImGui.PopID();
			}
			ImGui.EndCombo();
		}
		ImGui.EndDisabled();
		ImGui.SetItemTooltip( "{0}", "Pick an online player" );
	}

	private static string Hint( AdminCommandArgumentRow argument )
	{
		if ( argument.Kind is not (CommandArgumentKind.Integer or CommandArgumentKind.Number) ) return argument.Remainder ? "Text" : "Value";
		var kind = argument.Kind == CommandArgumentKind.Integer ? "Whole number" : "Number";
		return argument.Minimum is { } min && argument.Maximum is { } max ? $"{kind}, {min}–{max}" : kind;
	}

	/// <summary>Collects values in order; returns what is missing or wrong, or null when the command can run.</summary>
	private string BuildArguments( AdminCommandRow command, out string[] values )
	{
		var collected = new List<string>();
		var skipped = "";
		foreach ( var argument in command.Arguments )
		{
			var value = _commandValues.GetValueOrDefault( command.Name + "/" + argument.Name, "" ).Trim();
			if ( value.Length == 0 )
			{
				if ( !argument.Optional ) { values = Array.Empty<string>(); return $"Fill in {argument.Name}."; }
				if ( skipped.Length == 0 ) skipped = argument.Name;
				continue;
			}
			// Arguments are positional, so a later value cannot follow an empty optional one.
			if ( skipped.Length > 0 ) { values = Array.Empty<string>(); return $"Fill in {skipped} before {argument.Name}."; }
			if ( argument.Kind is CommandArgumentKind.Integer or CommandArgumentKind.Number && !double.TryParse( value,
				System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _ ) )
			{
				values = Array.Empty<string>();
				return $"{argument.Name} must be a number.";
			}
			collected.Add( value );
		}
		values = collected.ToArray();
		return null;
	}

	private void DrawCommandResult( AdminReply result )
	{
		ImGui.TextColored( AdminMenuTheme.Tone( result.Status ), "{0}", result.Status.ToString() );
		ImGui.SameLine();
		ImGui.TextWrapped( "{0}", result.Message );
		if ( result.Data.Count == 0 ) return;

		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable;
		if ( !ImGui.BeginTable( "command-result", 2, flags ) ) return;
		ImGui.TableSetupColumn( "Field", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "retentionPending" ).x );
		ImGui.TableSetupColumn( "Value", ImGuiTableColumnFlags.WidthStretch );
		foreach ( var (field, value) in result.Data.OrderBy( pair => ResultOrder( pair.Key ) ) )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextDisabled( "{0}", field );
			ImGui.TableNextColumn();
			ImGui.TextWrapped( "{0}", value );
		}
		ImGui.EndTable();
	}

	// Listing rows (row1..row8) come first in their natural order, then the remaining fields alphabetically.
	private static string ResultOrder( string key ) =>
		key.StartsWith( "row", StringComparison.Ordinal ) && int.TryParse( key.AsSpan( 3 ), out var row ) ? $"0{row:D3}" : "1" + key;
}
