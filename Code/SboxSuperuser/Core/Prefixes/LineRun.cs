using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Core.Prefixes;

/// <summary>A stretch of a rendered line in one style. Gap is empty space before it, in letter widths.</summary>
internal readonly record struct LineRun( string Text, EffectColor Color, float Gap, bool Bold, bool Italic );
