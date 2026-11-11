using Sandbox;

namespace SboxSuperuser.Systems;

/// <summary>Draws the latest csay message in the middle of the screen, then fades it out.</summary>
internal sealed class CenterTextSystem : GameObjectSystem<CenterTextSystem>
{
	private const float VisibleSeconds = 6;
	private const float FadeSeconds = 1;
	private const float FontSize = 32;
	private const float HeightFraction = 0.3f;

	private string _text;
	private RealTimeSince _shown;

	public CenterTextSystem( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 95, Draw, nameof( CenterTextSystem ) );

	internal void Show( string text )
	{
		_text = text;
		_shown = 0;
	}

	private void Draw()
	{
		if ( _text is null ) return;
		if ( _shown > VisibleSeconds ) { _text = null; return; }
		if ( Scene.Camera is not { } camera ) return;
		var alpha = Math.Clamp( (VisibleSeconds - _shown) / FadeSeconds, 0, 1 );
		using var hud = camera.BeginHud();
		hud.TextStyle = TextStyle.Default.WithSize( FontSize ).WithColor( Color.White.WithAlpha( alpha ) )
			.WithAlignment( TextFlag.Center ).WithShadow();
		var size = camera.ScreenRect.Size;
		hud.Text( _text, new Rect( 0, size.y * HeightFraction - FontSize, size.x, FontSize * 2 ) );
	}
}
