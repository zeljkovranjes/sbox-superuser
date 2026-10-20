namespace SboxSuperuser.Core.Effects;

/// <summary>What each built-in function does. Help text and argument counts live in <see cref="EffectSymbols"/>.</summary>
internal static class EffectFunctions
{
	internal static readonly IReadOnlyDictionary<string, EffectFunction> All = new Dictionary<string, EffectFunction>( StringComparer.Ordinal )
	{
		[ "floor" ] = Number( "n", a => Math.Floor( a[ 0 ].Number ) ),
		[ "ceil" ] = Number( "n", a => Math.Ceiling( a[ 0 ].Number ) ),
		[ "round" ] = Number( "n", a => Math.Round( a[ 0 ].Number, MidpointRounding.AwayFromZero ) ),
		[ "abs" ] = Number( "n", a => Math.Abs( a[ 0 ].Number ) ),
		[ "sqrt" ] = Number( "n", a => a[ 0 ].Number > 0 ? Math.Sqrt( a[ 0 ].Number ) : 0 ),
		[ "pow" ] = Number( "nn", a => Math.Pow( a[ 0 ].Number, a[ 1 ].Number ) ),
		[ "sin" ] = Number( "n", a => Math.Sin( a[ 0 ].Number ) ),
		[ "cos" ] = Number( "n", a => Math.Cos( a[ 0 ].Number ) ),
		[ "tan" ] = Number( "n", a => Math.Tan( a[ 0 ].Number ) ),
		[ "min" ] = Number( "nn", a => Math.Min( a[ 0 ].Number, a[ 1 ].Number ) ),
		[ "max" ] = Number( "nn", a => Math.Max( a[ 0 ].Number, a[ 1 ].Number ) ),
		[ "clamp" ] = Number( "nnn", a => Math.Max( a[ 1 ].Number, Math.Min( a[ 2 ].Number, a[ 0 ].Number ) ) ),
		[ "lerp" ] = Number( "nnn", a => a[ 0 ].Number + (a[ 1 ].Number - a[ 0 ].Number) * a[ 2 ].Number ),
		[ "mod" ] = Number( "nn", a => Mod( a[ 0 ].Number, a[ 1 ].Number ) ),
		[ "sign" ] = Number( "n", a => Math.Sign( a[ 0 ].Number ) ),
		[ "rgb" ] = Color( "nnnn", a => EffectColor.FromBytes( a[ 0 ].Number, a[ 1 ].Number, a[ 2 ].Number, a.Length > 3 ? a[ 3 ].Number : 255 ) ),
		[ "hsv" ] = Color( "nnn", a => EffectColor.FromHsv( a[ 0 ].Number, a[ 1 ].Number, a[ 2 ].Number ) ),
		[ "mix" ] = Color( "ccn", a => a[ 0 ].Color.Mix( a[ 1 ].Color, a[ 2 ].Number ) ),
		[ "fade" ] = Color( "cn", a => a[ 0 ].Color.WithAlpha( a[ 1 ].Number ) ),
		[ "len" ] = Number( "s", a => a[ 0 ].Kind == EffectValueKind.List ? a[ 0 ].Items.Length : a[ 0 ].Text.Length ),
		[ "upper" ] = Text( "t", a => a[ 0 ].Text.ToUpperInvariant() ),
		[ "lower" ] = Text( "t", a => a[ 0 ].Text.ToLowerInvariant() ),
		[ "at" ] = Text( "tn", a => Letter( a[ 0 ].Text, a[ 1 ].Number ) ),
		[ "pingpong" ] = Number( "nn", a => PingPong( a[ 0 ].Number, a[ 1 ].Number ) ),
		[ "wave" ] = Number( "nn", a => a[ 1 ].Number == 0 ? 0 : 0.5 - 0.5 * Math.Cos( 2 * Math.PI * a[ 0 ].Number / a[ 1 ].Number ) ),
		[ "hash" ] = Number( "nn", a => Hash( a[ 0 ].Number, a.Length > 1 ? a[ 1 ].Number : 0 ) )
	};

	/// <summary>A remainder that is never negative, and 0 when dividing by 0.</summary>
	internal static double Mod( double value, double divisor )
	{
		if ( divisor == 0 ) return 0;
		var remainder = value % divisor;
		return remainder < 0 ? remainder + Math.Abs( divisor ) : remainder;
	}

	/// <summary>Index into a sequence, wrapping in both directions.</summary>
	internal static int Wrap( double index, int length ) => length == 0 ? 0 : (int)Mod( Math.Floor( index ), length );

	internal static string Letter( string text, double index ) => text.Length == 0 ? "" : text[ Wrap( index, text.Length ) ].ToString();

	// 0, 1 ... count-1, count-1 ... 1, 0: the WaW bounce, which pauses one step at each end.
	internal static double PingPong( double value, double count )
	{
		var steps = (int)Math.Floor( count );
		if ( steps <= 1 ) return 0;
		var phase = Wrap( value, steps * 2 );
		return phase < steps ? phase : steps * 2 - 1 - phase;
	}

	internal static double Hash( double a, double b )
	{
		unchecked
		{
			var value = (ulong)(long)Math.Floor( a ) * 0x9E3779B97F4A7C15UL ^ (ulong)(long)Math.Floor( b ) * 0xC2B2AE3D27D4EB4FUL;
			value ^= value >> 33;
			value *= 0xFF51AFD7ED558CCDUL;
			value ^= value >> 33;
			return (value >> 11) / (double)(1UL << 53);
		}
	}

	private static EffectFunction Number( string parameters, Func<EffectValue[], double> body ) =>
		new( parameters, EffectValueKind.Number, arguments => EffectValue.Of( Finite( body( arguments ) ) ) );

	private static EffectFunction Color( string parameters, Func<EffectValue[], EffectColor> body ) =>
		new( parameters, EffectValueKind.Color, arguments => EffectValue.Of( body( arguments ) ) );

	private static EffectFunction Text( string parameters, Func<EffectValue[], string> body ) =>
		new( parameters, EffectValueKind.Text, arguments => EffectValue.Of( body( arguments ) ) );

	internal static double Finite( double value ) => double.IsFinite( value ) ? value : 0;
}
