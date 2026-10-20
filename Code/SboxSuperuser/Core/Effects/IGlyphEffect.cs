namespace SboxSuperuser.Core.Effects;

/// <summary>Styles a part of a line letter by letter: a compiled script or a C# effect registered by game code.</summary>
internal interface IGlyphEffect
{
	/// <summary>False for effects that look the same every frame, so callers can skip redrawing them.</summary>
	bool IsAnimated { get; }

	/// <summary>Adds one glyph per visible letter; returns false (adding nothing) when the effect failed this frame.</summary>
	bool Render( in EffectInput input, List<EffectGlyph> output );
}
