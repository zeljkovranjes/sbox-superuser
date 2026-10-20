using System.Globalization;

namespace SboxSuperuser.Core.Effects;

/// <summary>An RGBA color with channels from 0 to 1, as effect scripts produce them.</summary>
internal readonly record struct EffectColor( float R, float G, float B, float A = 1 )
{
	internal static readonly EffectColor White = new( 1, 1, 1 );

	/// <summary>Parses #rgb, #rrggbb or #rrggbbaa; returns false for anything else.</summary>
	internal static bool TryParseHex( string text, out EffectColor color )
	{
		color = White;
		if ( string.IsNullOrEmpty( text ) || text[ 0 ] != '#' ) return false;
		var hex = text.Substring( 1 );
		if ( hex.Length == 3 ) hex = string.Concat( hex.Select( digit => new string( digit, 2 ) ) );
		if ( hex.Length is not (6 or 8) || !uint.TryParse( hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value ) ) return false;
		if ( hex.Length == 6 ) value = (value << 8) | 0xFF;
		color = new EffectColor( ((value >> 24) & 0xFF) / 255f, ((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f );
		return true;
	}

	internal static EffectColor FromBytes( double r, double g, double b, double a = 255 ) =>
		new( Channel( r / 255 ), Channel( g / 255 ), Channel( b / 255 ), Channel( a / 255 ) );

	/// <summary>Hue in degrees (wraps), saturation and value from 0 to 1.</summary>
	internal static EffectColor FromHsv( double hue, double saturation, double value )
	{
		hue = double.IsFinite( hue ) ? ((hue % 360) + 360) % 360 / 60 : 0;
		double v = Channel( value );
		var chroma = v * Channel( saturation );
		var x = chroma * (1 - Math.Abs( hue % 2 - 1 ));
		var m = v - chroma;
		(double r, double g, double b) = (int)hue switch
		{
			0 => (chroma, x, 0d),
			1 => (x, chroma, 0d),
			2 => (0d, chroma, x),
			3 => (0d, x, chroma),
			4 => (x, 0d, chroma),
			_ => (chroma, 0d, x)
		};
		return new EffectColor( (float)(r + m), (float)(g + m), (float)(b + m) );
	}

	internal EffectColor Mix( EffectColor other, double amount )
	{
		var t = Channel( amount );
		return new EffectColor( R + (other.R - R) * t, G + (other.G - G) * t, B + (other.B - B) * t, A + (other.A - A) * t );
	}

	internal EffectColor WithAlpha( double alpha ) => this with { A = Channel( alpha ) };

	internal string ToHex()
	{
		static int Byte( float channel ) => (int)MathF.Round( channel * 255 );
		var rgb = $"#{Byte( R ):X2}{Byte( G ):X2}{Byte( B ):X2}";
		return A >= 1 ? rgb : rgb + Byte( A ).ToString( "X2", CultureInfo.InvariantCulture );
	}

	private static float Channel( double value ) => (float)Math.Clamp( double.IsFinite( value ) ? value : 0, 0, 1 );
}
