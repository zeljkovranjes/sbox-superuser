namespace SboxSuperuser.Core.Effects;

/// <summary>One piece of script text: what it is, its exact text and where it sits in the source.</summary>
internal readonly record struct EffectToken( EffectTokenKind Kind, string Text, int Start, int Length )
{
	internal int End => Start + Length;
	internal bool Is( string text ) => Kind is EffectTokenKind.Operator or EffectTokenKind.Keyword && Text == text;
}
