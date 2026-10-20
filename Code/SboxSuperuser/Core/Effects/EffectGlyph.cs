namespace SboxSuperuser.Core.Effects;

/// <summary>One drawn letter (or replacement text) with its style; offset is in letter widths.</summary>
internal readonly record struct EffectGlyph( string Text, EffectColor Color, float Offset, bool Bold, bool Italic );
