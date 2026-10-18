using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A host-issued, revocable internal caller limited to explicit command permissions; it is never an owner.</summary>
public sealed class CommandService : IDisposable
{
	private readonly HashSet<string> _permissions;
	private readonly Func<CommandService, CommandRequest, CancellationToken, Task<CommandResult>> _execute;
	private bool _disposed;
	internal bool IsActive => !_disposed;

	/// <summary>Gets the stable service identity used to scope requests and audit evidence.</summary>
	public string Id { get; }

	internal CommandService( string id, IReadOnlyList<string> permissions,
		Func<CommandService, CommandRequest, CancellationToken, Task<CommandResult>> execute )
	{
		Id = ContractGuard.Key( id, nameof( id ) );
		if ( id.Length > 96 || permissions is null || permissions.Count > 64 )
		{
			throw new ArgumentException( "Service identity and permission set must be bounded." );
		}

		_permissions = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var permission in permissions )
		{
			ContractGuard.Key( permission, nameof( permissions ) );
			if ( permission.Length > 96 || !_permissions.Add( permission ) )
			{
				throw new ArgumentException( "Service permissions must be unique exact keys." );
			}
		}

		_execute = execute;
	}

	internal bool Allows( string permission ) => IsActive && _permissions.Contains( permission );

	/// <summary>Submits a request through the same authoritative dispatcher on the host main thread.</summary>
	public Task<CommandResult> ExecuteAsync( CommandRequest request, CancellationToken cancellation = default ) => _execute( this, request, cancellation );

	/// <summary>Revokes future and queued authority; already-started irreversible work cannot promise rollback.</summary>
	public void Dispose() => _disposed = true;
}
