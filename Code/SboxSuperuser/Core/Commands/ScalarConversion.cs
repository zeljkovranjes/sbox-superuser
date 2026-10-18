using System.Globalization;

namespace SboxSuperuser.Core.Commands;

internal static class ScalarConversion
{
	internal static bool Integer( string text, double? minimum, double? maximum, out long value )
	{
		return long.TryParse( text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value )
			&& (!minimum.HasValue || AtLeast( value, minimum.Value ))
			&& (!maximum.HasValue || AtMost( value, maximum.Value ));
	}

	private static bool AtLeast( long value, double bound ) => bound <= long.MinValue
		|| bound < 9223372036854775808d && value >= (long)Math.Ceiling( bound );

	private static bool AtMost( long value, double bound ) => bound >= 9223372036854775808d
		|| bound >= long.MinValue && value <= (long)Math.Floor( bound );

	internal static bool Number( string text, double? minimum, double? maximum, out double value )
	{
		return double.TryParse( text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
			CultureInfo.InvariantCulture, out value ) && double.IsFinite( value )
			&& (!minimum.HasValue || value >= minimum.Value) && (!maximum.HasValue || value <= maximum.Value);
	}

	internal static bool Boolean( string text, out bool value )
	{
		value = string.Equals( text, "true", StringComparison.OrdinalIgnoreCase );
		return value || string.Equals( text, "false", StringComparison.OrdinalIgnoreCase );
	}

	internal static bool Duration( string text, out TimeSpan? value )
	{
		value = null;
		if ( string.Equals( text, "permanent", StringComparison.OrdinalIgnoreCase ) )
		{
			return true;
		}

		if ( string.IsNullOrEmpty( text ) || text.Length < 2 || text[ 0 ] == '0'
			|| !long.TryParse( text.Substring( 0, text.Length - 1 ), NumberStyles.None, CultureInfo.InvariantCulture, out var amount ) || amount <= 0 )
		{
			return false;
		}

		var ticks = char.ToLowerInvariant( text[ text.Length - 1 ] ) switch
		{
			's' => TimeSpan.TicksPerSecond,
			'm' => TimeSpan.TicksPerMinute,
			'h' => TimeSpan.TicksPerHour,
			'd' => TimeSpan.TicksPerDay,
			'w' => 7 * TimeSpan.TicksPerDay,
			_ => 0
		};
		if ( ticks == 0 || amount > long.MaxValue / ticks )
		{
			return false;
		}

		value = TimeSpan.FromTicks( amount * ticks );
		return true;
	}
}
