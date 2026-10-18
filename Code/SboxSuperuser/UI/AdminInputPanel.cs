// Adapted from sbox-imgui's ImGuiInputPanel; see Engine/Vendor/ImGui/LICENSE.
using Sandbox;
using Sandbox.UI;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed class AdminInputPanel : RootPanel
{
	private readonly ImGuiIO _io;
	internal Action Close { get; set; }

	internal AdminInputPanel( ImGuiIO io )
	{
		_io = io;
		Style.PointerEvents = PointerEvents.All;
		Style.Position = PositionMode.Absolute;
		Style.Width = Length.Percent( 100 );
		Style.Height = Length.Percent( 100 );
		Style.ZIndex = 10000;
		AcceptsFocus = true;
		RenderedManually = false;
		Focus();
	}

	public override void OnButtonEvent( ButtonEvent e )
	{
		if ( e.Button == "escape" && e.Pressed ) Close?.Invoke();
		switch ( e.Button )
		{
			case "mouseleft": _io.AddMouseButtonEvent( 0, e.Pressed ); break;
			case "mouseright": _io.AddMouseButtonEvent( 1, e.Pressed ); break;
			case "mousemiddle": _io.AddMouseButtonEvent( 2, e.Pressed ); break;
			default:
				var key = ImGuiKeyNames.FromSboxName( e.Button );
				if ( key != ImGuiKey.None ) _io.AddKeyEvent( key, e.Pressed );
				break;
		}
		e.StopPropagation = true;
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		var key = ImGuiKeyNames.FromSboxName( e.Button );
		if ( key != ImGuiKey.None ) _io.AddKeyTyped( key, e.HasCtrl, e.HasShift, e.HasAlt );
		e.StopPropagation = true;
	}

	public override void OnKeyTyped( char key )
	{
		if ( key >= 32 ) _io.AddInputCharacter( key );
	}

	public override void OnPaste( string text )
	{
		if ( text is not null ) _io.AddPasteEvent( text.Length <= 256 ? text : text.Substring( 0, 256 ) );
	}

	public override string GetClipboardValue( bool cut ) => ImGuiHost.ClipboardCopyProvider?.Invoke( cut );

	public override void OnMouseWheel( Vector2 value ) => _io.AddMouseWheelEvent( -value.x, -value.y );
}
