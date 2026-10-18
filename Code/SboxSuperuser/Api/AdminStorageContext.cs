namespace SboxSuperuser;

/// <summary>Host-only factory inputs; editor and dedicated data must use separate namespaces.</summary>
public sealed class AdminStorageContext
{
	/// <summary>Gets whether this is an actual dedicated server; false means opted-in editor testing.</summary>
	public bool IsDedicatedServer { get; }

	/// <summary>Gets the existing local backend for decoration or explicitly selected local testing.</summary>
	public IAdminStorageBackend LocalStore { get; }

	internal AdminStorageContext( bool dedicated, IAdminStorageBackend local )
	{
		IsDedicatedServer = dedicated;
		LocalStore = local;
	}
}
