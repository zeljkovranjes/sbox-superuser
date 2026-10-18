using System.Collections.ObjectModel;
using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A host-created snapshot of parsed values, never raw client-authored objects.</summary>
public sealed class CommandArguments
{
	private readonly IReadOnlyDictionary<string, object> _values;

	internal CommandArguments( IReadOnlyDictionary<string, object> values )
	{
		if ( values is null )
		{
			throw new ArgumentNullException( nameof( values ) );
		}

		var copy = new Dictionary<string, object>( StringComparer.Ordinal );
		foreach ( var pair in values )
		{
			ContractGuard.Key( pair.Key, nameof( values ) );
			if ( pair.Value is not (string or long or double or bool or Enum or CommandDuration or PlayerReference)
				|| pair.Value is double number && !double.IsFinite( number ) )
			{
				throw new ArgumentException( "Only supported immutable argument values are accepted.", nameof( values ) );
			}

			copy.Add( pair.Key, pair.Value );
		}

		_values = new ReadOnlyDictionary<string, object>( copy );
	}

	/// <summary>Tests whether an optional argument was supplied.</summary>
	public bool Contains( string name ) => _values.ContainsKey( name );

	/// <summary>Gets the declared value; missing keys and type mismatches throw instead of returning defaults.</summary>
	public T Get<T>( string name )
	{
		if ( !_values.TryGetValue( name, out var value ) )
		{
			throw new ArgumentException( "Argument was not supplied.", nameof( name ) );
		}

		if ( value is not T typed )
		{
			throw new ArgumentException( "Argument type does not match its descriptor.", nameof( name ) );
		}

		return typed;
	}
}
