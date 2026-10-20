namespace SboxSuperuser.Core.Effects;

/// <summary>Where each built-in input and output lives in a running program; script variables come after these.</summary>
internal static class EffectSlots
{
	internal const int Time = 0;
	internal const int Index = 1;
	internal const int Count = 2;
	internal const int Letter = 3;
	internal const int Part = 4;
	internal const int Text = 5;
	internal const int Palette = 6;
	internal const int Seed = 7;
	internal const int Color = 8;
	internal const int Alpha = 9;
	internal const int Char = 10;
	internal const int Offset = 11;
	internal const int Bold = 12;
	internal const int Italic = 13;
	internal const int Hidden = 14;
	internal const int Name = 15;
	internal const int Prefix = 16;
	internal const int Fixed = 17;

	internal static readonly string[] Names =
	{
		"t", "i", "L", "ch", "part", "text", "palette", "seed",
		"color", "alpha", "char", "offset", "bold", "italic", "hidden",
		"%name%", "%prefix%"
	};

	internal static bool IsOutput( int slot ) => slot is >= Color and <= Hidden;

	internal static bool IsReadOnly( int slot ) => slot < Fixed && !IsOutput( slot );

	internal static EffectType TypeOf( int slot ) => slot switch
	{
		Time or Index or Count or Seed or Alpha or Offset => EffectType.Of( EffectValueKind.Number ),
		Letter or Part or Text or Char or Name or Prefix => EffectType.Of( EffectValueKind.Text ),
		Palette => new EffectType( EffectValueKind.List, EffectValueKind.Color ),
		Color => EffectType.Of( EffectValueKind.Color ),
		_ => EffectType.Of( EffectValueKind.Bool )
	};
}
