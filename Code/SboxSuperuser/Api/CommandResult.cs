using System.Collections.ObjectModel;
using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Private caller-safe outcome data; constructing this value performs no action.</summary>
public sealed class CommandResult
{
	/// <summary>Gets the explicit outcome, including Unknown for indeterminate actions.</summary>
	public CommandStatus Status { get; }

	/// <summary>Gets safe caller-facing text; never include secrets or internal exceptions.</summary>
	public string Message { get; }

	/// <summary>Gets optional caller-safe structured string fields as an immutable snapshot.</summary>
	public IReadOnlyDictionary<string, string> Data { get; }

	/// <summary>Creates an outcome value; callers remain responsible for accurately reporting observed work.</summary>
	public CommandResult( CommandStatus status, string message, IReadOnlyDictionary<string, string> data = null )
	{
		Status = ContractGuard.EnumValue( status, nameof( status ) );
		Message = ContractGuard.Text( message, nameof( message ) );
		var copy = new Dictionary<string, string>( StringComparer.Ordinal );
		if ( data is not null )
		{
			foreach ( var pair in data )
			{
				copy.Add( ContractGuard.Text( pair.Key, nameof( data ) ),
					pair.Value ?? throw new ArgumentException( "Result fields cannot be null.", nameof( data ) ) );
			}
		}

		Data = new ReadOnlyDictionary<string, string>( copy );
	}
}
