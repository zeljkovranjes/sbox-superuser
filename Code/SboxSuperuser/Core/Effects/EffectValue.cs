using System.Globalization;

namespace SboxSuperuser.Core.Effects;

/// <summary>A value inside an effect script: a number, true/false, text, a color or a list.</summary>
internal readonly struct EffectValue
{
	internal EffectValueKind Kind { get; }
	internal double Number { get; }
	internal string Text { get; }
	internal EffectColor Color { get; }
	internal EffectValue[] Items { get; }

	private EffectValue( EffectValueKind kind, double number = 0, string text = null, EffectColor color = default, EffectValue[] items = null )
	{
		Kind = kind;
		Number = number;
		Text = text ?? "";
		Color = color;
		Items = items;
	}

	internal static readonly EffectValue Zero = new( EffectValueKind.Number );
	internal static EffectValue Of( double number ) => new( EffectValueKind.Number, number );
	internal static EffectValue Of( bool value ) => new( EffectValueKind.Bool, value ? 1 : 0 );
	internal static EffectValue Of( string text ) => new( EffectValueKind.Text, text: text );
	internal static EffectValue Of( EffectColor color ) => new( EffectValueKind.Color, color: color );
	internal static EffectValue Of( EffectValue[] items ) => new( EffectValueKind.List, items: items );

	/// <summary>Numbers other than zero, non-empty text, colors and non-empty lists count as true.</summary>
	internal bool IsTrue => Kind switch
	{
		EffectValueKind.Number or EffectValueKind.Bool => Number != 0,
		EffectValueKind.Text => Text.Length > 0,
		EffectValueKind.List => Items.Length > 0,
		_ => true
	};

	internal static string Describe( EffectValueKind kind ) => kind switch
	{
		EffectValueKind.Number => "a number",
		EffectValueKind.Bool => "true or false",
		EffectValueKind.Text => "text",
		EffectValueKind.Color => "a color",
		_ => "a list"
	};

	public override string ToString() => Kind switch
	{
		EffectValueKind.Number => Number.ToString( CultureInfo.InvariantCulture ),
		EffectValueKind.Bool => Number != 0 ? "true" : "false",
		EffectValueKind.Text => Text,
		EffectValueKind.Color => Color.ToHex(),
		_ => "[" + string.Join( ", ", Items.Select( item => item.ToString() ) ) + "]"
	};
}
