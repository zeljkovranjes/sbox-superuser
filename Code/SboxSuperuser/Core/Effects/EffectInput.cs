namespace SboxSuperuser.Core.Effects;

/// <summary>Everything one part of a line gives its effect: the text to style plus the shared clock and player data.</summary>
internal readonly record struct EffectInput( string Text, string Part, double Time, int Seed, EffectValue Palette, string Name, string Prefix );
