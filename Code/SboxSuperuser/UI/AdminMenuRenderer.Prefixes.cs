using Sandbox;
using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Effects;
using SboxSuperuser.Core.Prefixes;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Prefixes;
using SboxSuperuser.Engine.Vendor.ImGui;
using SboxSuperuser.Systems;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string PrefixExportCommand = "prefix.export";

	private readonly List<LineRun> _prefixRuns = new();
	private string _prefixSelected = "";
	private PrefixDefinition _draftSource;
	private string _draftName = "";
	private string _draftTag = "";
	private string _draftTemplate = "";
	private List<Vector4> _draftPalette = new();
	private string _newPrefixId = "";
	private string _sampleName;
	private bool _confirmPrefixDelete;

	internal PrefixEditorModel PrefixEditor { get; } = new( Superuser.RequestAdminCommand );

	private PrefixSystem PrefixSystem => _scene.GetSystem<PrefixSystem>();

	private string SampleName => _sampleName ??= Connection.Local?.DisplayName is { Length: > 0 } name ? name : "Player";

	private static string LocalSteamId => Connection.Local?.SteamId.ToString() ?? "";

	private void DrawPrefixesWindow()
	{
		if ( !BeginWindow( PrefixesWindow, "Prefixes", new Vector2( 160, 70 ), new Vector2( 900, 600 ), new Vector2( 620, 380 ), out var expanded ) ) return;
		if ( expanded ) DrawPrefixesContent();
		ImGui.End();
	}

	private void DrawPrefixesContent()
	{
		if ( !Server.Can( PrefixExportCommand ) )
		{
			Caption( "Editing prefixes needs the admin.prefixes.manage permission, and the Prefix module must be on." );
			return;
		}
		PrefixEditor.Request();
		var catalog = PrefixEditor.Catalog;
		if ( catalog is null )
		{
			if ( PrefixEditor.Problem is { } problem ) ColoredWrapped( problem, AdminMenuTheme.Danger );
			else Caption( "Loading prefixes..." );
			return;
		}
		DrawPrefixStatus();

		var available = ImGui.GetContentRegionAvail();
		ImGui.BeginChild( "prefix-list", new Vector2( MathF.Max( 230 * Scale, available.x * 0.3f ), 0 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX );
		DrawPrefixList( catalog );
		ImGui.EndChild();
		ImGui.SameLine();
		ImGui.BeginChild( "prefix-detail", Vector2.Zero, ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding );
		if ( ImGui.BeginTabBar( "prefix-tabs" ) )
		{
			if ( ImGui.BeginTabItem( "Prefix" ) )
			{
				var selected = catalog.Find( _prefixSelected );
				if ( selected is null ) Caption( "Pick a prefix on the left, or create one." );
				else DrawPrefixDetail( selected );
				ImGui.EndTabItem();
			}
			if ( ImGui.BeginTabItem( "Groups" ) )
			{
				DrawPrefixGroups( catalog );
				ImGui.EndTabItem();
			}
			if ( ImGui.BeginTabItem( "Players" ) )
			{
				DrawPrefixPlayers( catalog );
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
		ImGui.EndChild();
	}

	private void DrawPrefixStatus()
	{
		if ( PrefixEditor.Busy ) Caption( "Saving..." );
		else if ( PrefixEditor.LastResult is { } result && result.Status is not (CommandStatus.Success or CommandStatus.NoChange) )
			ColoredWrapped( result.Message, AdminMenuTheme.Tone( result.Status ) );
	}

	private void DrawPrefixList( PrefixCatalog catalog )
	{
		ImGui.SetNextItemWidth( -80 * Scale );
		ImGui.InputTextWithHint( "##new-prefix", "new-id", ref _newPrefixId );
		ImGui.SameLine();
		if ( ImGui.Button( "New" ) && ValidPrefixId( _newPrefixId, catalog ) )
		{
			PrefixEditor.Queue( "prefix.create", new[] { _newPrefixId } );
			_prefixSelected = _newPrefixId;
			_newPrefixId = "";
		}
		ImGui.Separator();

		var now = PrefixSystem.Now;
		foreach ( var prefix in catalog.Prefixes )
		{
			ImGui.PushID( prefix.Id );
			if ( ImGui.Selectable( "##row", _prefixSelected == prefix.Id, ImGuiSelectableFlags.AllowOverlap, new Vector2( 0, ImGui.GetTextLineHeight() * 2 + 4 ) ) )
			{
				_prefixSelected = prefix.Id;
				_confirmPrefixDelete = false;
			}
			var bottom = ImGui.GetCursorPos();
			ImGui.SameLine( 6 );
			ImGui.BeginGroup();
			DrawPrefixPreview( prefix, SampleName, now );
			ImGui.TextDisabled( "{0}", prefix.Id );
			ImGui.EndGroup();
			ImGui.SetCursorPos( bottom );
			ImGui.PopID();
		}
		if ( catalog.Prefixes.Count == 0 ) Caption( "No prefixes yet. Type an ID above and press New." );
	}

	private void DrawPrefixPreview( PrefixDefinition prefix, string name, double now, IGlyphEffect tag = null, IGlyphEffect nameEffect = null )
	{
		var effects = PrefixSystem.Effects;
		_prefixRuns.Clear();
		PrefixSystem.Render( prefix, tag ?? effects.Resolve( prefix.TagStyle ), nameEffect ?? effects.Resolve( prefix.NameStyle ), name, LocalSteamId, now, _prefixRuns );
		StyledText.Draw( _prefixRuns );
	}

	private void DrawPrefixDetail( PrefixDefinition prefix )
	{
		LoadDraft( prefix );
		var draft = DraftDefinition( prefix, out var problem );

		ImGui.PushFont( null, 24 );
		DrawPrefixPreview( draft ?? prefix, SampleName, PrefixSystem.Now );
		ImGui.PopFont();
		ImGui.SetNextItemWidth( 220 * Scale );
		var sample = SampleName;
		if ( ImGui.InputText( "Preview name", ref sample, 64 ) ) _sampleName = sample;
		ImGui.Separator();

		ImGui.InputText( "Name", ref _draftName, PrefixDefinition.MaximumNameLength );
		ImGui.InputText( "Tag", ref _draftTag, PrefixDefinition.MaximumTagLength );
		ImGui.InputText( "Template", ref _draftTemplate, PrefixTemplate.MaximumLength );
		Caption( "The template places the tag and the name: %prefix% is the tag, %name% the player's name." );
		DrawPaletteEditor();
		if ( problem is not null ) ColoredWrapped( problem, AdminMenuTheme.Danger );

		var dirty = draft is not null && !PrefixCodec.Same( draft, prefix );
		ImGui.BeginDisabled( !dirty || PrefixEditor.Busy );
		if ( ImGui.Button( "Apply" ) ) ApplyDraft( prefix, draft );
		ImGui.EndDisabled();
		ImGui.SameLine();
		ImGui.BeginDisabled( !dirty );
		if ( ImGui.Button( "Revert" ) ) _draftSource = null;
		ImGui.EndDisabled();

		ImGui.SeparatorText( "Effects" );
		DrawStylePicker( prefix, "tag", prefix.TagStyle );
		DrawStylePicker( prefix, "name", prefix.NameStyle );

		ImGui.SeparatorText( "Manage" );
		if ( ImGui.Button( "Duplicate" ) ) DuplicatePrefix( prefix );
		ImGui.SameLine();
		if ( !_confirmPrefixDelete && ImGui.Button( "Delete" ) ) _confirmPrefixDelete = true;
		if ( _confirmPrefixDelete )
		{
			ImGui.PushStyleColor( ImGuiCol.Button, AdminMenuTheme.Danger );
			if ( ImGui.Button( "Delete for good" ) )
			{
				PrefixEditor.Queue( "prefix.delete", new[] { prefix.Id } );
				_confirmPrefixDelete = false;
			}
			ImGui.PopStyleColor();
			ImGui.SameLine();
			if ( ImGui.Button( "Keep it" ) ) _confirmPrefixDelete = false;
		}
	}

	private void LoadDraft( PrefixDefinition prefix )
	{
		if ( ReferenceEquals( _draftSource, prefix ) ) return;
		var reload = _draftSource is null || _draftSource.Id != prefix.Id;
		_draftSource = prefix;
		if ( !reload ) return;
		_draftName = prefix.Name;
		_draftTag = prefix.Tag;
		_draftTemplate = prefix.Template;
		_draftPalette = prefix.Palette.Select( hex => EffectColor.TryParseHex( hex, out var color ) ? new Vector4( color.R, color.G, color.B, 1 ) : Vector4.One ).ToList();
	}

	private PrefixDefinition DraftDefinition( PrefixDefinition prefix, out string problem )
	{
		problem = null;
		try
		{
			return prefix.With( _draftName, _draftTag, _draftTemplate, palette: _draftPalette.Select( Hex ).ToArray() );
		}
		catch ( ArgumentException error )
		{
			problem = error.Message.Split( " (Parameter" )[ 0 ];
			return null;
		}
	}

	private void DrawPaletteEditor()
	{
		ImGui.AlignTextToFramePadding();
		ImGui.TextUnformatted( "Palette" );
		ImGui.SameLine();
		for ( var index = 0; index < _draftPalette.Count; index++ )
		{
			ImGui.PushID( index );
			var color = _draftPalette[ index ];
			if ( ImGui.ColorEdit3( "##color", ref color, ImGuiColorEditFlags.NoInputs ) ) _draftPalette[ index ] = color;
			if ( ImGui.IsItemClicked( ImGuiMouseButton.Right ) ) _draftPalette.RemoveAt( index-- );
			ImGui.SameLine();
			ImGui.PopID();
		}
		ImGui.BeginDisabled( _draftPalette.Count >= PrefixDefinition.MaximumPaletteColors );
		if ( ImGui.SmallButton( "+" ) ) _draftPalette.Add( _draftPalette.Count > 0 ? _draftPalette[ ^1 ] : Vector4.One );
		ImGui.EndDisabled();
		Caption( _draftPalette.Count == 0
			? "No palette: effects use the WaW colors red, green, yellow, blue and cyan. Press + to start your own."
			: "Click a color to change it, right-click to remove it. Effects read it as palette[0], palette[1] ..." );
	}

	private void ApplyDraft( PrefixDefinition saved, PrefixDefinition draft )
	{
		if ( draft.Name != saved.Name ) PrefixEditor.Queue( "prefix.update", new[] { saved.Id, "name", draft.Name } );
		if ( draft.Tag != saved.Tag ) PrefixEditor.Queue( "prefix.update", new[] { saved.Id, "tag", draft.Tag } );
		if ( draft.Template != saved.Template ) PrefixEditor.Queue( "prefix.update", new[] { saved.Id, "template", draft.Template } );
		if ( !draft.Palette.SequenceEqual( saved.Palette ) )
			PrefixEditor.Queue( "prefix.update", new[] { saved.Id, "palette", string.Join( ",", draft.Palette ) } );
		_draftSource = null;
	}

	private void DrawStylePicker( PrefixDefinition prefix, string part, PrefixStyle style )
	{
		ImGui.PushID( part );
		ImGui.AlignTextToFramePadding();
		ImGui.TextUnformatted( part == "tag" ? "Tag effect " : "Name effect" );
		ImGui.SameLine();
		ImGui.SetNextItemWidth( 240 * Scale );
		if ( ImGui.BeginCombo( "##style", DescribeStyle( style ) ) )
		{
			foreach ( var preset in EffectPresets.All )
			{
				if ( ImGui.Selectable( preset.Id, style.Kind == PrefixStyleKind.Preset && style.Value == preset.Id ) )
					QueueStyle( prefix.Id, part, new PrefixStyle( PrefixStyleKind.Preset, preset.Id ) );
				ImGui.SameLine();
				ImGui.TextDisabled( "{0}", preset.Title );
			}
			foreach ( var effect in PrefixSystem.Effects.CodeEffects )
			{
				if ( ImGui.Selectable( "code:" + effect.Id, style.Kind == PrefixStyleKind.Code && style.Value == effect.Id ) )
					QueueStyle( prefix.Id, part, new PrefixStyle( PrefixStyleKind.Code, effect.Id ) );
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		if ( ImGui.Button( "Edit code" ) ) OpenEffectEditor( prefix, part, style );
		ImGui.PopID();
	}

	private string DescribeStyle( PrefixStyle style ) => style.Kind switch
	{
		PrefixStyleKind.Preset => $"Preset: {EffectPresets.Find( style.Value )?.Title ?? style.Value}",
		PrefixStyleKind.Code => PrefixSystem.Effects.CodeEffects.FirstOrDefault( effect => effect.Id == style.Value ) is { } effect
			? $"C#: {effect.Title}" : $"C# (not on this machine): {style.Value}",
		_ => $"Custom script ({style.Value.Split( '\n' ).Length} lines)"
	};

	private void QueueStyle( string prefixId, string part, PrefixStyle style )
	{
		IReadOnlyList<string> arguments;
		try { arguments = PrefixStyleText.Arguments( style ); }
		catch ( ArgumentException error )
		{
			Notify( error.Message, AdminMenuTheme.Danger );
			return;
		}
		PrefixEditor.Queue( "prefix.update", new[] { prefixId, part + "-effect" }.Concat( arguments ) );
	}

	private void DuplicatePrefix( PrefixDefinition prefix )
	{
		var catalog = PrefixEditor.Catalog;
		var copy = Enumerable.Range( 2, 50 ).Select( number => $"{prefix.Id}-{number}" )
			.FirstOrDefault( id => id.Length <= PrefixDefinition.MaximumIdLength && catalog.Find( id ) is null );
		if ( copy is null ) { Notify( "Pick a shorter ID for the copy.", AdminMenuTheme.Warning ); return; }
		PrefixEditor.Queue( "prefix.create", new[] { copy } );
		PrefixEditor.Queue( "prefix.update", new[] { copy, "name", prefix.Name + " copy" } );
		PrefixEditor.Queue( "prefix.update", new[] { copy, "tag", prefix.Tag } );
		PrefixEditor.Queue( "prefix.update", new[] { copy, "template", prefix.Template } );
		if ( prefix.Palette.Count > 0 ) PrefixEditor.Queue( "prefix.update", new[] { copy, "palette", string.Join( ",", prefix.Palette ) } );
		QueueStyle( copy, "tag", prefix.TagStyle );
		QueueStyle( copy, "name", prefix.NameStyle );
		_prefixSelected = copy;
	}

	private void DrawPrefixGroups( PrefixCatalog catalog )
	{
		Caption( "Every member of a group wears its prefix. A player in several groups gets the one with the highest protection; owner means every owner." );
		if ( ImGui.SmallButton( "Refresh" ) ) PrefixEditor.Refresh();
		ImGui.SameLine();
		Caption( "New groups from the Permissions window show up after a refresh." );

		// Assignments for groups that were deleted stay listed, so they can be cleared.
		var known = PrefixEditor.Groups;
		var removed = catalog.Groups.Keys.Where( id => known.All( group => group.GroupId != id ) ).OrderBy( id => id, StringComparer.Ordinal );
		var rows = known.Select( group => (group.GroupId, Protection: (int?)group.Protection) )
			.Concat( removed.Select( id => (GroupId: id, Protection: (int?)null) ) ).ToArray();
		if ( !ImGui.BeginTable( "prefix-groups", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp ) ) return;
		ImGui.TableSetupColumn( "Group", ImGuiTableColumnFlags.WidthFixed, 150 * Scale );
		ImGui.TableSetupColumn( "Protection", ImGuiTableColumnFlags.WidthFixed, 80 * Scale );
		ImGui.TableSetupColumn( "Prefix", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableHeadersRow();
		foreach ( var (group, protection) in rows )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.AlignTextToFramePadding();
			ImGui.TextUnformatted( group );
			ImGui.TableNextColumn();
			ImGui.AlignTextToFramePadding();
			if ( protection is { } level ) ImGui.TextDisabled( "{0}", level );
			else ImGui.TextColored( AdminMenuTheme.Warning, "{0}", "deleted" );
			ImGui.TableNextColumn();
			ImGui.PushID( group );
			catalog.Groups.TryGetValue( group, out var current );
			if ( AssignmentCombo( catalog, current, "None" ) is { } choice )
			{
				if ( choice.Length == 0 ) PrefixEditor.Queue( "prefix.group.clear", new[] { group } );
				else PrefixEditor.Queue( "prefix.group.set", new[] { group, choice } );
			}
			ImGui.PopID();
		}
		ImGui.EndTable();
	}

	private void DrawPrefixPlayers( PrefixCatalog catalog )
	{
		Caption( "A player's own prefix beats their group prefixes. Pick Group prefix to hand them back to their groups." );
		var online = Model.Players?.Players ?? Array.Empty<AdminPlayerRow>();
		var steamIds = online.Select( player => player.SteamId ).Concat( catalog.Users.Keys ).Distinct( StringComparer.Ordinal ).ToArray();
		if ( !ImGui.BeginTable( "prefix-players", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp ) ) return;
		foreach ( var steamId in steamIds )
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.AlignTextToFramePadding();
			var name = online.FirstOrDefault( player => player.SteamId == steamId )?.Name;
			if ( name is null ) ImGui.TextDisabled( "{0} (offline)", steamId );
			else ImGui.TextUnformatted( name );
			ImGui.TableNextColumn();
			ImGui.PushID( steamId );
			catalog.Users.TryGetValue( steamId, out var current );
			if ( AssignmentCombo( catalog, current, "Group prefix" ) is { } choice )
			{
				if ( choice.Length == 0 ) PrefixEditor.Queue( "prefix.user.clear", new[] { steamId } );
				else PrefixEditor.Queue( "prefix.user.set", new[] { steamId, choice } );
			}
			ImGui.PopID();
		}
		ImGui.EndTable();
		if ( steamIds.Length == 0 ) Caption( "Open the Players window to list who is online." );
	}

	// The selected player's prefix, shown in the Players window.
	private void DrawPlayerPrefix()
	{
		var steamId = Model.Selected;
		var name = Model.Players?.Players.FirstOrDefault( player => player.SteamId == steamId )?.Name ?? steamId;
		_prefixRuns.Clear();
		PrefixSystem.Render( steamId, name, PrefixSystem.Now, _prefixRuns );
		StyledText.Draw( _prefixRuns );
		PrefixEditor.Request();
		var catalog = PrefixEditor.Catalog;
		if ( catalog is null )
		{
			Caption( PrefixEditor.Problem ?? "Loading prefixes..." );
			return;
		}
		catalog.Users.TryGetValue( steamId, out var own );
		Caption( own is null ? "Wears their group prefix. Pick one to give them their own." : "Has a prefix of their own." );
		ImGui.PushID( "player-prefix" );
		if ( AssignmentCombo( catalog, own, "Group prefix" ) is { } choice )
		{
			if ( choice.Length == 0 ) PrefixEditor.Queue( "prefix.user.clear", new[] { steamId } );
			else PrefixEditor.Queue( "prefix.user.set", new[] { steamId, choice } );
		}
		ImGui.PopID();
		DrawPrefixStatus();
	}

	/// <summary>A prefix picker; returns the chosen ID, "" for the empty choice, or null when nothing was picked.</summary>
	private static string AssignmentCombo( PrefixCatalog catalog, string current, string emptyLabel )
	{
		string choice = null;
		ImGui.SetNextItemWidth( -1 );
		if ( !ImGui.BeginCombo( "##assign", current ?? emptyLabel ) ) return null;
		if ( ImGui.Selectable( emptyLabel, current is null ) && current is not null ) choice = "";
		foreach ( var prefix in catalog.Prefixes )
		{
			if ( ImGui.Selectable( prefix.Id, prefix.Id == current ) && prefix.Id != current ) choice = prefix.Id;
			ImGui.SameLine();
			ImGui.TextDisabled( "{0}", prefix.Tag );
		}
		ImGui.EndCombo();
		return choice;
	}

	private bool ValidPrefixId( string id, PrefixCatalog catalog )
	{
		try
		{
			ContractGuard.Key( id, nameof( id ) );
			if ( id.Length > PrefixDefinition.MaximumIdLength ) throw new ArgumentException( "Too long." );
		}
		catch ( ArgumentException )
		{
			Notify( "Prefix IDs are short lowercase words like vip or staff.vip, starting with a letter.", AdminMenuTheme.Warning );
			return false;
		}
		if ( catalog.Find( id ) is null ) return true;
		Notify( $"A prefix called {id} already exists.", AdminMenuTheme.Warning );
		return false;
	}

	private static string Hex( Vector4 color ) => new EffectColor( color.x, color.y, color.z ).ToHex().ToLowerInvariant();
}
