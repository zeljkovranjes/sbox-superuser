namespace SboxSuperuser.Core.Effects;

/// <summary>What the checker knows about a value before the script runs; a null kind means "only known at run time".</summary>
internal readonly record struct EffectType( EffectValueKind? Kind, EffectValueKind? Item = null )
{
	internal static readonly EffectType Unknown = new( null );

	internal static EffectType Of( EffectValueKind kind ) => new( kind );

	internal bool IsNumeric => Kind is EffectValueKind.Number or EffectValueKind.Bool;

	internal EffectType Merge( EffectType other ) => this == other ? this
		: IsNumeric && other.IsNumeric ? Of( EffectValueKind.Number ) : Unknown;
}
