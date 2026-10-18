using Sandbox;

namespace SboxSuperuser.Engine.CommandDiscovery;

internal sealed class HandlerParameter
{
	internal CommandArgumentDescriptor Descriptor { get; }
	private readonly Type _type;
	private readonly object _defaultValue;
	private readonly Dictionary<string, object> _enumValues;

	internal HandlerParameter( Type type, string name, bool optional, object defaultValue, AdminArgumentAttribute metadata )
	{
		_type = type;
		_defaultValue = defaultValue;
		var kind = Kind( type );
		_enumValues = new Dictionary<string, object>( StringComparer.OrdinalIgnoreCase );
		if ( kind == CommandArgumentKind.Enum )
		{
			foreach ( var entry in TypeLibrary.GetEnumDescription( type ) )
			{
				_enumValues.Add( entry.Name, entry.ObjectValue );
			}
		}

		var minimum = metadata is null || double.IsNaN( metadata.Minimum ) ? (double?)null : metadata.Minimum;
		var maximum = metadata is null || double.IsNaN( metadata.Maximum ) ? (double?)null : metadata.Maximum;
		if ( type == typeof( int ) )
		{
			minimum = Math.Max( minimum ?? int.MinValue, int.MinValue );
			maximum = Math.Min( maximum ?? int.MaxValue, int.MaxValue );
		}

		if ( type == typeof( float ) )
		{
			minimum = Math.Max( minimum ?? -float.MaxValue, -float.MaxValue );
			maximum = Math.Min( maximum ?? float.MaxValue, float.MaxValue );
		}

		Descriptor = new CommandArgumentDescriptor( metadata?.Name ?? name.ToLowerInvariant(), metadata?.Description ?? name,
			kind, optional, metadata?.IsRemainder ?? false, minimum, maximum, _enumValues.Keys,
			metadata?.TargetPolicy ?? CommandTargetPolicy.None, metadata?.Audit ?? CommandArgumentAudit.Redacted );
		if ( optional && defaultValue is null && type != typeof( string )
			|| optional && kind == CommandArgumentKind.Enum && !_enumValues.Values.Contains( defaultValue ) )
		{
			throw new ArgumentException( "Optional defaults must be representable by the declared parameter type." );
		}

		if ( optional && kind is CommandArgumentKind.Integer or CommandArgumentKind.Number )
		{
			var number = Convert.ToDouble( defaultValue, System.Globalization.CultureInfo.InvariantCulture );
			if ( !double.IsFinite( number ) || number < minimum || number > maximum )
			{
				throw new ArgumentException( "The optional default violates the declared numeric bounds." );
			}
		}
	}

	internal object Bind( CommandArguments arguments )
	{
		if ( !arguments.Contains( Descriptor.Name ) && Descriptor.IsOptional )
		{
			return _defaultValue;
		}

		var value = arguments.Get<object>( Descriptor.Name );
		if ( _type == typeof( int ) )
		{
			return checked((int)(long)value);
		}

		if ( _type == typeof( float ) )
		{
			return (float)(double)value;
		}

		if ( Descriptor.Kind == CommandArgumentKind.Enum )
		{
			return _enumValues[ (string)value ];
		}

		return value;
	}

	private static CommandArgumentKind Kind( Type type )
	{
		if ( type == typeof( string ) )
		{
			return CommandArgumentKind.String;
		}

		if ( type == typeof( int ) || type == typeof( long ) )
		{
			return CommandArgumentKind.Integer;
		}

		if ( type == typeof( float ) || type == typeof( double ) )
		{
			return CommandArgumentKind.Number;
		}

		if ( type == typeof( bool ) )
		{
			return CommandArgumentKind.Boolean;
		}

		if ( type == typeof( CommandDuration ) )
		{
			return CommandArgumentKind.Duration;
		}

		if ( type == typeof( PlayerReference ) )
		{
			return CommandArgumentKind.Player;
		}

		if ( TypeLibrary.GetType( type )?.IsEnum == true )
		{
			return CommandArgumentKind.Enum;
		}

		throw new ArgumentException( $"Unsupported parameter type '{type.Name}'." );
	}
}
