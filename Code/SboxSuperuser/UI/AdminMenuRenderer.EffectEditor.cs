using Sandbox;
using SboxSuperuser.Core.Effects;
using SboxSuperuser.Core.Prefixes;
using SboxSuperuser.Engine.Prefixes;
using SboxSuperuser.Engine.Vendor.ImGui;
using SboxSuperuser.Systems;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private EffectEdit _effectEdit;
	private Vector4 _pickedColor = new( 1, 0.53f, 0, 1 );

	private void OpenEffectEditor( PrefixDefinition prefix, string part, PrefixStyle style )
	{
		CloseEffectEditor();
		var source = style.Kind == PrefixStyleKind.Code ? EffectPresets.Templates[ 0 ].Source : style.Source;
		_effectEdit = new EffectEdit( prefix.Id, part, new EffectCodeEditor( source ), style.Kind == PrefixStyleKind.Preset ? style.Value : null );
		_effectEdit.Editor.SetFocused( true );
	}

	private void CloseEffectEditor()
	{
		_effectEdit?.Editor.SetFocused( false );
		_effectEdit = null;
	}

	private void DrawEffectEditor()
	{
		if ( _effectEdit is null || !Interactive ) return;
		var edit = _effectEdit;
		var prefix = PrefixEditor.Catalog?.Find( edit.PrefixId );
		if ( prefix is null )
		{
			CloseEffectEditor();
			return;
		}

		ImGui.SetNextWindowPos( new Vector2( 220, 110 ) * Scale, ImGuiCond.FirstUseEver );
		ImGui.SetNextWindowSize( new Vector2( 900, 620 ) * Scale, ImGuiCond.FirstUseEver );
		ImGui.SetNextWindowSizeConstraints( new Vector2( 620, 420 ) * Scale, new Vector2( float.MaxValue, float.MaxValue ) );
		var open = true;
		if ( ImGui.Begin( "Effect editor###effect-editor", ref open, ImGuiWindowFlags.NoSavedSettings ) ) DrawEffectEditorContent( edit, prefix );
		ImGui.End();
		if ( !open ) CloseEffectEditor();
	}

	private void DrawEffectEditorContent( EffectEdit edit, PrefixDefinition prefix )
	{
		ImGui.TextUnformatted( edit.Part == "tag" ? "Tag effect of " : "Name effect of " );
		ImGui.SameLine( 0, 0 );
		ImGui.TextColored( CodeColor, "{0}", prefix.Id );
		if ( edit.Preset is not null )
			Caption( $"Started from the {edit.Preset} preset. Saving stores your version as a custom script; the preset never changes." );
		DrawEffectToolbar( edit );

		var compiled = edit.Editor.Compilation;
		var program = compiled.Program;
		var effects = PrefixSystem.Effects;
		ImGui.Spacing();
		ImGui.PushFont( null, 26 );
		DrawPrefixPreview( prefix, SampleName, PrefixSystem.Now,
			edit.Part == "tag" ? program : effects.Resolve( prefix.TagStyle ),
			edit.Part == "name" ? program : effects.Resolve( prefix.NameStyle ) );
		ImGui.PopFont();
		if ( program?.LastError is { } running ) ColoredWrapped( "While running, " + running.Describe( compiled.Source ), AdminMenuTheme.Warning );
		ImGui.Spacing();

		var available = ImGui.GetContentRegionAvail();
		var side = 240 * Scale;
		var bar = ImGui.GetFrameHeightWithSpacing() * 3.2f;
		edit.Editor.Draw( "code", new Vector2( available.x - side - ImGui.GetStyle().ItemSpacing.x, available.y - bar ) );
		ImGui.SameLine();
		ImGui.BeginChild( "builtins", new Vector2( side, available.y - bar ), ImGuiChildFlags.Borders );
		DrawBuiltIns( edit.Editor );
		ImGui.EndChild();

		DrawProblems( compiled );
		ImGui.BeginDisabled( !compiled.Succeeded || PrefixEditor.Busy );
		if ( ImGui.Button( "Save" ) )
		{
			QueueStyle( prefix.Id, edit.Part, PrefixStyle.Create( PrefixStyleKind.Script, edit.Editor.Buffer.Text ) );
			CloseEffectEditor();
		}
		ImGui.EndDisabled();
		ImGui.SameLine();
		if ( ImGui.Button( "Cancel" ) ) CloseEffectEditor();
		ImGui.SameLine();
		Caption( "Ctrl+Z undo · Ctrl+Y redo · Tab indents · Esc leaves the editor" );
	}

	private void DrawEffectToolbar( EffectEdit edit )
	{
		ImGui.SetNextItemWidth( 190 * Scale );
		if ( ImGui.BeginCombo( "##template", "New from template" ) )
		{
			foreach ( var template in EffectPresets.Templates )
			{
				if ( ImGui.Selectable( template.Id ) ) edit.Editor.Load( template.Source );
				if ( ImGui.IsItemHovered() ) ImGui.SetTooltip( "{0}: {1}", template.Title, template.Description );
				ImGui.SameLine();
				ImGui.TextDisabled( "{0}", template.Title );
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		ImGui.SetNextItemWidth( 170 * Scale );
		if ( ImGui.BeginCombo( "##preset", "Copy a preset" ) )
		{
			foreach ( var preset in EffectPresets.All )
			{
				if ( ImGui.Selectable( preset.Id ) ) edit.Editor.Load( preset.Source );
				if ( ImGui.IsItemHovered() ) ImGui.SetTooltip( "{0}", preset.Description );
				ImGui.SameLine();
				ImGui.TextDisabled( "{0}", preset.Title );
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		ImGui.ColorEdit3( "##pick", ref _pickedColor, ImGuiColorEditFlags.NoInputs );
		ImGui.SameLine();
		if ( ImGui.Button( "Insert color" ) ) edit.Editor.InsertAtCursor( Hex( _pickedColor ) );
		ImGui.SameLine();
		if ( ImGui.Button( "Reference" ) ) OpenEffectReference();
	}

	private static void DrawProblems( EffectCompilation compiled )
	{
		var count = compiled.Diagnostics.Count;
		if ( count == 0 ) ImGui.TextColored( AdminMenuTheme.Success, "{0}", "No problems." );
		foreach ( var diagnostic in compiled.Diagnostics.Take( 2 ) )
			ImGui.TextColored( AdminMenuTheme.Danger, "{0}", diagnostic.Describe( compiled.Source ) );
		if ( count > 2 ) ImGui.TextDisabled( "and {0} more", count - 2 );
		ImGui.SameLine();
		ImGui.TextDisabled( "{0} / {1} characters", compiled.Source.Length, EffectProgram.MaximumSourceLength );
	}

	private static void DrawBuiltIns( EffectCodeEditor editor )
	{
		Caption( "Click to insert. Hover for help." );
		BuiltInSection( editor, "Outputs", EffectSymbolKind.Output, true );
		BuiltInSection( editor, "Inputs", EffectSymbolKind.Input, true );
		BuiltInSection( editor, "Placeholders", EffectSymbolKind.Placeholder, false );
		BuiltInSection( editor, "Functions", EffectSymbolKind.Function, false );
		if ( !ImGui.CollapsingHeader( "Colors" ) ) return;
		foreach ( var (name, color) in EffectSymbols.Colors )
		{
			var start = ImGui.GetCursorScreenPos();
			var size = ImGui.GetTextLineHeight();
			ImGui.GetWindowDrawList().AddRectFilled( start + new Vector2( 0, 2 ), start + new Vector2( size - 4, size - 2 ),
				ImGui.GetColorU32( EffectColors.ToColor( color ) ), 2 );
			ImGui.SetCursorScreenPos( start + new Vector2( size, 0 ) );
			if ( ImGui.Selectable( name ) ) editor.InsertAtCursor( name );
		}
	}

	private static void BuiltInSection( EffectCodeEditor editor, string title, EffectSymbolKind kind, bool open )
	{
		if ( !ImGui.CollapsingHeader( title, open ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None ) ) return;
		foreach ( var symbol in EffectSymbols.All.Where( symbol => symbol.Kind == kind ) )
		{
			if ( ImGui.Selectable( symbol.Name ) ) editor.InsertAtCursor( kind == EffectSymbolKind.Function ? symbol.Name + "(" : symbol.Name );
			if ( ImGui.IsItemHovered() ) ImGui.SetTooltip( "{0}\n\nExample: {1}", symbol.Help, symbol.Example );
		}
	}

	private void OpenEffectReference()
	{
		Server.RequestDocumentation();
		var pages = Server.Documentation?.Modules.FirstOrDefault( module => module.Id == PrefixModule.Id )?.Pages;
		var page = pages is null ? -1 : Array.IndexOf( pages, "Effect reference" );
		_docModule = PrefixModule.Id;
		_docSection = page >= 0 ? page : 2;
		_layout.SetOpen( DocumentationWindow, true );
	}

	private sealed record EffectEdit( string PrefixId, string Part, EffectCodeEditor Editor, string Preset );
}
