using Sandbox;

namespace SboxSuperuser.Components;

/// <summary>
/// Superuser's scoreboard, shown while the score key is held: every player's avatar, prefix, name and ping, styled
/// like Superuser's chat. Right-click with the cursor free (hold the key and press the right mouse button) for a
/// player's Steam profile, and staff also get kick, warn, mute, gag and ban. Not part of the bootstrap; add it to
/// use it.
/// </summary>
[Title( "Superuser Scoreboard" ), Category( "Superuser" ), Icon( "leaderboard" )]
public sealed class SuperuserScoreboard : Component
{
	/// <summary>The input action that shows the scoreboard while held.</summary>
	[Property, InputAction] public string ShowAction { get; set; } = "score";

	private UI.ScoreboardRenderer _renderer;
	private bool _cursorFree;
	private MouseVisibility _previousMouse;

	protected override void OnUpdate()
	{
		if ( !Networking.IsActive || !Input.Down( ShowAction ) )
		{
			Release();
			_renderer?.Clear();
			return;
		}
		_renderer ??= new UI.ScoreboardRenderer( Scene );
		// The right mouse button frees the cursor so a row can be right-clicked.
		if ( !_cursorFree && Input.Pressed( "attack2" ) )
		{
			_cursorFree = true;
			_previousMouse = Mouse.Visibility;
		}
		if ( _cursorFree )
		{
			Mouse.Visibility = MouseVisibility.Visible;
			_renderer.AddMouseButton( 0, Input.Keyboard.Down( "mouse1" ) );
			_renderer.AddMouseButton( 1, Input.Keyboard.Down( "mouse2" ) );
		}
		_renderer.Draw( _cursorFree );
	}

	private void Release()
	{
		if ( !_cursorFree ) return;
		_cursorFree = false;
		Mouse.Visibility = _previousMouse;
	}

	protected override void OnDestroy()
	{
		Release();
		_renderer?.Dispose();
		_renderer = null;
	}
}
