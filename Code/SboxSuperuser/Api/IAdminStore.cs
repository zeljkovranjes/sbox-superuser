namespace SboxSuperuser;

/// <summary>Host-local versioned aggregate storage; call from the engine main thread on the authoritative host.</summary>
public interface IAdminStore
{
	/// <summary>Reads a compatible generation; distinguishes missing, corrupt, unsupported and unavailable data.</summary>
	Task<StoreReadResult> ReadAsync( StoreKey key, int supportedSchemaVersion, CancellationToken cancellation );

	/// <summary>Serializes a recoverable replacement after validating JSON/schema and expected revision.</summary>
	Task<StoreWriteResult> WriteAsync( StoreWriteRequest request, CancellationToken cancellation );
}
