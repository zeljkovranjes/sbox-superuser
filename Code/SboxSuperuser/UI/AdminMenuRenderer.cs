using Sandbox;
using Sandbox.Rendering;
using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer : IDisposable
{
	private readonly Scene _scene;
	private readonly ImGuiContext _context = new();
	private readonly CommandList _commands = new( "Superuser admin menu" ) { Flags = CommandList.Flag.Hud };
	private readonly AdminInputPanel _input;
	private readonly MouseVisibility _previousMouse;
	private CameraComponent _camera;
	internal AdminMenuModel Model { get; } = new( Superuser.RequestAdminQuery, Superuser.RequestAdminAction );
	internal bool Open = true;

	internal AdminMenuRenderer( Scene scene )
	{
		_scene = scene;
		_previousMouse = Mouse.Visibility;
		_input = new AdminInputPanel( _context.IO ) { Close = () => Open = false };
		AdminMenuTheme.Apply( _context.Style );
	}

	internal void CaptureInput()
	{
		Mouse.Visibility = MouseVisibility.Visible;
		Input.ClearActions();
		Input.AnalogMove = 0;
		Input.AnalogLook = default;
		if ( Input.EscapePressed )
		{
			Input.EscapePressed = false;
			Open = false;
		}
	}

	internal void Draw()
	{
		Model.Tick( DateTimeOffset.UtcNow );
		var previous = ImGuiContext.Current;
		ImGuiContext.Current = _context;
		try
		{
			_context.IO.DeltaTime = MathF.Max( RealTime.Delta, 0.0001f );
			_context.IO.MousePos = Mouse.Position;
			ImGui.NewFrame();
			DrawWindow();
			var lists = ImGui.Render();
			if ( _camera != _scene.Camera )
			{
				if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
				_camera = _scene.Camera;
				if ( _camera.IsValid() ) _camera.AddCommandList( _commands, Sandbox.Rendering.Stage.AfterUI, 10000 );
			}
			_commands.Reset();
			using var painter = Painter.Begin( _commands );
			foreach ( var list in lists ) list.Render( painter );
		}
		finally { ImGuiContext.Current = previous; }
	}

	public void Dispose()
	{
		_input.Delete();
		_commands.Reset();
		if ( _camera.IsValid() ) _camera.RemoveCommandList( _commands );
		if ( Mouse.Visibility == MouseVisibility.Visible ) Mouse.Visibility = _previousMouse;
		ImGuiHost.ClipboardCopyProvider = null;
		if ( ReferenceEquals( ImGuiContext.Current, _context ) ) ImGuiContext.Current = null;
	}
}
