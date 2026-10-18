namespace SboxSuperuser.Core.Contracts;

internal static class ContractGuard
{
	internal static string Text( string value, string parameter )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
		{
			throw new ArgumentException( "A nonempty value is required.", parameter );
		}

		return value;
	}

	internal static string Key( string value, string parameter )
	{
		Text( value, parameter );
		var segmentStart = true;
		foreach ( var character in value )
		{
			if ( character == '.' && !segmentStart )
			{
				segmentStart = true;
				continue;
			}

			if ( character is not ( >= 'a' and <= 'z' )
				&& (segmentStart || character is not ( >= '0' and <= '9' or '_' or '-' )) )
			{
				throw new ArgumentException( "Use lowercase dotted keys; each segment starts with a letter.", parameter );
			}

			segmentStart = false;
		}

		if ( segmentStart )
		{
			throw new ArgumentException( "A key cannot end in a dot.", parameter );
		}

		return value;
	}

	internal static IReadOnlyList<T> Copy<T>( IEnumerable<T> values, string parameter )
	{
		if ( values is null )
		{
			throw new ArgumentNullException( parameter );
		}

		var copy = values.ToArray();
		if ( copy.Any( value => value is null ) )
		{
			throw new ArgumentException( "Null entries are not supported.", parameter );
		}

		return Array.AsReadOnly( copy );
	}

	internal static T EnumValue<T>( T value, string parameter ) where T : struct, Enum
	{
		if ( !Enum.IsDefined( typeof(T), value ) )
		{
			throw new ArgumentOutOfRangeException( parameter );
		}

		return value;
	}
}
