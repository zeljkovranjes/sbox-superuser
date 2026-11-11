using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	readonly Dictionary<string, SettingsFields> _settingsFields = new();

	void DrawModuleSettings( AdminMenuElementRow element, string key )
	{
		var command = FindCommand( element.Command );
		if ( command is null ) return;
		var reply = Server.Live( key, element.StatusCommand, Array.Empty<string>(), DateTimeOffset.UtcNow );
		ImGui.Spacing();
		ImGui.BeginGroup();
		ImGui.SeparatorText( Label( element.Text ) );
		Caption( command.Description );
		ImGui.Spacing();
		if ( reply is not { Status: CommandStatus.Success } )
		{
			if ( reply is null ) Caption( "Loading current settings…" );
			else ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) );
			ImGui.EndGroup();
			return;
		}
		if ( !_settingsFields.TryGetValue( key, out var fields ) )
		{
			fields = new SettingsFields( command, element );
			_settingsFields.Add( key, fields );
			SeedSettings( command, reply, fields );
		}
		fields.Refresh( reply );
		ImGui.PushID( command.Name );
		if ( ImGui.BeginTable( "fields", 2, ImGuiTableFlags.SizingStretchProp ) )
		{
			ImGui.TableSetupColumn( "Setting", ImGuiTableColumnFlags.WidthStretch, 0.56f );
			ImGui.TableSetupColumn( "Value", ImGuiTableColumnFlags.WidthStretch, 0.44f );
			for ( var i = 0; i < command.Arguments.Length; i++ ) DrawSetting( command.Arguments[i], fields, i );
			ImGui.EndTable();
		}
		ImGui.Spacing();
		DrawSettingsActions( element, command, reply, fields );
		ImGui.PopID();
		ImGui.EndGroup();
	}

	void SeedSettings( AdminCommandRow command, AdminReply reply, SettingsFields fields )
	{
		for ( var i = 0; i < command.Arguments.Length; i++ )
			_commandValues[fields.Keys[i]] = reply.Data.GetValueOrDefault( command.Arguments[i].Name, "" );
	}

	void DrawSetting( AdminCommandArgumentRow argument, SettingsFields fields, int index )
	{
		ImGui.PushID( argument.Name );
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.AlignTextToFramePadding();
		ImGui.TextUnformatted( fields.Labels[index] );
		ImGui.SetItemTooltip( "{0}", argument.Description );
		Caption( fields.Defaults[index] );
		ImGui.TableNextColumn();
		var valueKey = fields.Keys[index];
		var value = _commandValues.GetValueOrDefault( valueKey, "" );
		if ( argument.Kind == CommandArgumentKind.Boolean )
		{
			var enabled = string.Equals( value, "true", StringComparison.OrdinalIgnoreCase );
			if ( ImGui.Checkbox( "##value", ref enabled ) ) value = enabled ? "true" : "false";
		}
		else Field( "##value", fields.Hints[index], ref value, 256, ImGui.GetContentRegionAvail().x );
		if ( value != _commandValues.GetValueOrDefault( valueKey, "" ) ) fields.Error = null;
		_commandValues[valueKey] = value;
		Caption( fields.Current[index] );
		ImGui.PopID();
	}

	void DrawSettingsActions( AdminMenuElementRow element, AdminCommandRow command, AdminReply reply, SettingsFields fields )
	{
		var missing = false;
		for ( var i = 0; i < fields.Keys.Length; i++ ) missing |= string.IsNullOrWhiteSpace( _commandValues.GetValueOrDefault( fields.Keys[i], "" ) );
		var changed = false;
		for ( var i = 0; i < command.Arguments.Length; i++ )
			changed |= _commandValues.GetValueOrDefault( fields.Keys[i], "" ) != reply.Data.GetValueOrDefault( command.Arguments[i].Name, "" );
		ImGui.BeginDisabled( Server.Running || missing || !changed );
		if ( ImGui.Button( "Apply changes", new Vector2( 130 * Scale, 0 ) ) )
		{
			fields.Error = BuildArguments( command, out var values );
			if ( fields.Error is null ) RunCommand( $"Apply {element.Text}?", command.Name, values, confirm: command.Mutation,
				details: command.Arguments.Zip( values ).Select( pair => (pair.First.Name, pair.Second) ).ToArray() );
		}
		ImGui.EndDisabled();
		ImGui.SameLine();
		if ( ImGui.Button( "Use current" ) ) SeedSettings( command, reply, fields );
		if ( element.Arguments.Length == command.Arguments.Length )
		{
			ImGui.SameLine();
			if ( ImGui.Button( "Defaults" ) )
				for ( var i = 0; i < command.Arguments.Length; i++ ) _commandValues[fields.Keys[i]] = element.Arguments[i];
		}
		if ( fields.Error is not null ) ColoredWrapped( fields.Error, AdminMenuTheme.Warning );
		else if ( !changed ) Caption( "Up to date" );
		if ( Server.LastCommand == command.Name && Server.LastResult is { } result ) ColoredWrapped( result.Message, AdminMenuTheme.Tone( result.Status ) );
	}

	sealed class SettingsFields
	{
		internal readonly string[] Keys, Labels, Defaults, Hints, Current;
		readonly AdminCommandRow _command;
		AdminReply _reply;
		internal string Error;

		internal SettingsFields( AdminCommandRow command, AdminMenuElementRow element )
		{
			_command = command;
			var count = command.Arguments.Length;
			Keys = new string[count]; Labels = new string[count]; Defaults = new string[count];
			Hints = new string[count]; Current = new string[count];
			for ( var i = 0; i < count; i++ )
			{
				var argument = command.Arguments[i];
				Keys[i] = command.Name + "/" + argument.Name;
				var label = argument.Name.Replace( '-', ' ' );
				Labels[i] = char.ToUpperInvariant( label[0] ) + label[1..];
				Defaults[i] = i < element.Arguments.Length ? "Default: " + element.Arguments[i] : "";
				Hints[i] = Hint( argument );
			}
		}

		internal void Refresh( AdminReply reply )
		{
			if ( ReferenceEquals( _reply, reply ) ) return;
			_reply = reply;
			for ( var i = 0; i < Current.Length; i++ )
				Current[i] = "Current: " + reply.Data.GetValueOrDefault( _command.Arguments[i].Name, "—" );
		}
	}
}
