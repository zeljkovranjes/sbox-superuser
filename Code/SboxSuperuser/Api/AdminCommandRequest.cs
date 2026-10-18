using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A registered command run from an admin view; the host authorizes, audits and executes it like console input.</summary>
public sealed class AdminCommandRequest
{
	private const int MaximumArguments = 16;
	private const int MaximumArgumentLength = 256;

	/// <summary>Gets the exact command key, such as "warn" or "group.permission.allow".</summary>
	public string Command { get; }

	/// <summary>Gets the ordered argument values, matching the command's argument descriptors.</summary>
	public IReadOnlyList<string> Arguments { get; }

	/// <summary>Creates bounded input; parsing, permission and target checks stay on the host.</summary>
	public AdminCommandRequest( string command, IEnumerable<string> arguments = null )
	{
		Command = ContractGuard.Key( command, nameof( command ) );
		var values = (arguments ?? Array.Empty<string>()).ToArray();
		if ( values.Length > MaximumArguments
			|| values.Any( value => value is null || value.Length > MaximumArgumentLength || value.Any( char.IsControl ) ) )
		{
			throw new ArgumentException( $"At most {MaximumArguments} printable arguments of up to {MaximumArgumentLength} characters.", nameof( arguments ) );
		}
		Arguments = Array.AsReadOnly( values );
	}

	internal CommandRequest ToCommand( Guid requestId ) => new( Command, Arguments, requestId );
}
