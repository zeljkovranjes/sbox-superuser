namespace SboxSuperuser.Core.Effects;

/// <summary>
/// A built-in function. <see cref="Parameters"/> has one letter per parameter: n number, c color, t text,
/// s text or list. Arguments are checked against it before <see cref="Body"/> runs.
/// </summary>
internal sealed record EffectFunction( string Parameters, EffectValueKind Returns, Func<EffectValue[], EffectValue> Body );
