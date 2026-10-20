using Sandbox;
using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Engine.Prefixes;

/// <summary>Converts between effect colors and s&amp;box colors.</summary>
internal static class EffectColors
{
	internal static Color ToColor( EffectColor color ) => new( color.R, color.G, color.B, color.A );

	internal static EffectColor FromColor( Color color ) => new( color.r, color.g, color.b, color.a );
}
