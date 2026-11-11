using Sandbox;
using SboxSuperuser.Core.Chat;
using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

/// <summary>The input line: typing and sending, autocomplete above it, and its Tab, Up and Down handling.</summary>
internal sealed partial class ChatBoxRenderer
{
	// Worked out again only when the typed line changes.
	private string _completedDraft;
	private Completion _completion = Completion.None;
	private static readonly Vector4 Hint = Rgb( 0x6E6E6E );
	private static readonly Vector4 HintCurrent = Rgb( 0xE8E8E8 );
	private const float PopoverGap = 4;
	// The popover's text, built once per change of the typed line rather than every frame.
	private string _signatureTyped = "", _signatureCurrent = "", _signatureRest = "", _argumentHelp = "";
	private int _selected;

	private void DrawInput()
	{
		var inputTopLeft = ImGui.GetCursorScreenPos();
		var inputWidth = ImGui.GetContentRegionAvail().x;
		ImGui.SetNextItemWidth( -1 );
		var opening = _focusInput;
		if ( _focusInput )
		{
			ImGui.SetKeyboardFocusHere();
			_focusInput = false;
		}
		var submitted = ImGui.InputTextWithHint( "##say", "Type a message...", ref _draft,
			ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.CallbackCompletion, EditCallback );
		// Enter still sends and closes once the input has lost focus, such as after a click in the history. Not on the
		// opening frame, in case the Enter that opened the chat reaches this box too.
		if ( !submitted && !opening && !ImGui.IsItemActive() )
			submitted = ImGui.IsKeyPressed( ImGuiKey.Enter, false ) || ImGui.IsKeyPressed( ImGuiKey.KeypadEnter, false );
		if ( _draft.Length > MaximumMessageLength ) _draft = _draft.Substring( 0, MaximumMessageLength );
		DrawCompletion( inputTopLeft, inputWidth );
		if ( !submitted ) return;
		var text = _draft.Trim();
		if ( text.Length > 0 )
		{
			Sandbox.Platform.Chat.Say( text );
			_history.Add( text );
		}
		Close();
	}

	/// <summary>
	/// Autocomplete as a small popover floating just above the input: the command with the argument being typed
	/// lit up, a line saying what it wants, and the suggestions, the selected one marked Tab. Up and Down move the
	/// selection. It overlays the history, so nothing else moves as it comes and goes.
	/// </summary>
	private void DrawCompletion( Vector2 inputTopLeft, float width )
	{
		if ( _completedDraft != _draft )
		{
			_completedDraft = _draft;
			_completion = _complete?.Invoke( _draft ) ?? Completion.None;
			_selected = 0;
			BuildSignature( _completion );
		}
		var completion = _completion;
		if ( completion.IsEmpty ) return;
		ImGui.SetNextWindowPos( inputTopLeft - new Vector2( 0, PopoverGap ), ImGuiCond.Always, new Vector2( 0, 1 ) );
		ImGui.SetNextWindowSizeConstraints( new Vector2( width * 0.5f, 0 ), new Vector2( width, float.MaxValue ) );
		ImGui.PushStyleColor( ImGuiCol.PopupBg, Raised );
		ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, new Vector2( 8, 6 ) );
		// A tooltip always draws above the focused chat window and never takes input, which is all this needs.
		if ( ImGui.BeginTooltip() )
		{
			if ( completion.Current is not null )
			{
				if ( _signatureTyped.Length > 0 )
				{
					ImGui.TextColored( Muted, "{0}", _signatureTyped );
					ImGui.SameLine();
				}
				ImGui.TextColored( HintCurrent, "{0}", _signatureCurrent );
				if ( _signatureRest.Length > 0 )
				{
					ImGui.SameLine();
					ImGui.TextColored( Hint, "{0}", _signatureRest );
				}
				if ( _argumentHelp.Length > 0 ) ImGui.TextColored( Hint, "{0}", _argumentHelp );
			}
			for ( var index = 0; index < completion.Suggestions.Count; index++ )
			{
				var selected = index == _selected;
				var row = ImGui.GetCursorScreenPos();
				if ( selected )
					ImGui.GetWindowDrawList().AddRectFilled( row - new Vector2( 4, 1 ), row + new Vector2( ImGui.GetContentRegionAvail().x + 4, ImGui.GetTextLineHeight() + 1 ),
						ImGui.GetColorU32( Hover ), 3 );
				ImGui.TextColored( selected ? HintCurrent : Muted, "{0}", completion.Suggestions[ index ] );
				var note = completion.NoteFor( index );
				if ( note.Length > 0 )
				{
					ImGui.SameLine();
					// Long notes are cut off before the Tab label rather than running into it.
					var start = ImGui.GetCursorScreenPos();
					var end = ImGui.GetWindowPos().x + ImGui.GetContentRegionMax().x - ImGui.CalcTextSize( "Tab" ).x - 8;
					ImGui.PushClipRect( start, new Vector2( end, start.y + ImGui.GetTextLineHeight() ), true );
					ImGui.TextColored( Hint, "{0}", note );
					ImGui.PopClipRect();
				}
				if ( !selected ) continue;
				ImGui.SameLine( ImGui.GetContentRegionMax().x - ImGui.CalcTextSize( "Tab" ).x );
				ImGui.TextColored( Hint, "{0}", "Tab" );
			}
			ImGui.EndTooltip();
		}
		ImGui.PopStyleVar();
		ImGui.PopStyleColor();
	}

	// "/mute <user> <duration> <reason>" split into what's typed, the argument being typed and the rest.
	private void BuildSignature( Completion completion )
	{
		_signatureTyped = completion.Typed;
		_signatureCurrent = completion.Current is { } current ? ArgumentLabel( current ) : "";
		_signatureRest = string.Join( " ", completion.Remaining.Select( ArgumentLabel ) );
		_argumentHelp = completion.Current?.Description ?? "";
	}

	/// <summary>Works the hint out again on the next frame, such as when the command list arrives.</summary>
	internal void RefreshCompletion() => _completedDraft = null;

	private static string ArgumentLabel( CompletionArgument argument ) => argument.Optional ? $"[{argument.Name}]" : $"<{argument.Name}>";

	private int EditCallback( ImGuiInputTextCallbackData data )
	{
		if ( data.EventFlag == ImGuiInputTextFlags.CallbackCompletion ) return AcceptSuggestion( data );
		// While suggestions show, Up and Down choose among them; otherwise they browse sent messages.
		var count = _completion.Suggestions.Count;
		if ( count == 0 ) return BrowseHistory( data );
		_selected = (_selected + (data.EventKey == ImGuiKey.UpArrow ? count - 1 : 1)) % count;
		return 0;
	}

	// Tab takes the selected suggestion and leaves the cursor ready for the next argument.
	private int AcceptSuggestion( ImGuiInputTextCallbackData data )
	{
		if ( _completion.Suggestions.Count == 0 ) return 0;
		var text = _completion.Accept( _completion.Suggestions[ Math.Min( _selected, _completion.Suggestions.Count - 1 ) ] );
		data.DeleteChars( 0, data.BufTextLen );
		data.InsertChars( 0, text );
		return 0;
	}

	// Up and Down swap the input for an earlier message, and back to what was being typed.
	private int BrowseHistory( ImGuiInputTextCallbackData data )
	{
		var text = data.EventKey == ImGuiKey.UpArrow ? _history.Older( data.Buf ) : _history.Newer();
		if ( text is null ) return 0;
		data.DeleteChars( 0, data.BufTextLen );
		data.InsertChars( 0, text );
		return 0;
	}
}
