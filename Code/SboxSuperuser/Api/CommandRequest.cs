using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Untrusted command payload; deliberately contains no actor, role, origin or services.</summary>
public sealed class CommandRequest
{
	/// <summary>Gets the command key, independent of chat/console roots.</summary>
	public string Command { get; }

	/// <summary>Gets a snapshot of raw tokens; transport bounds and parsing are still required.</summary>
	public IReadOnlyList<string> Arguments { get; }

	/// <summary>Gets the retry identifier; it carries no authority and is scoped by authenticated context.</summary>
	public Guid RequestId { get; }

	/// <summary>Creates payload data without dispatching a request.</summary>
	public CommandRequest( string command, IEnumerable<string> arguments, Guid requestId )
	{
		Command = ContractGuard.Key( command, nameof( command ) );
		Arguments = ContractGuard.Copy( arguments, nameof( arguments ) );
		if ( requestId == Guid.Empty )
		{
			throw new ArgumentException( "Request ID must be explicit.", nameof( requestId ) );
		}

		RequestId = requestId;
	}
}
