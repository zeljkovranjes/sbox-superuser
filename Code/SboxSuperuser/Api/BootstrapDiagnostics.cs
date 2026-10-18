namespace SboxSuperuser;

/// <summary>A local diagnostic snapshot with no owner list, capabilities or authorization token.</summary>
public sealed class BootstrapDiagnostics
{
	/// <summary>Gets current bootstrap health.</summary>
	public BootstrapState State { get; }

	/// <summary>Gets the current runtime ID, or null when no runtime is active.</summary>
	public Guid? RuntimeId { get; }

	/// <summary>Gets the number of enabled bootstrap registrations.</summary>
	public int BootstrapCount { get; }

	/// <summary>Gets the number of explicitly configured owners, never inferred owners.</summary>
	public int OwnerCount { get; }

	/// <summary>Gets the host-local configuration path for the selected mode.</summary>
	public string OwnerConfigurationPath { get; }

	/// <summary>Gets a safe diagnostic explanation.</summary>
	public string Message { get; }

	internal BootstrapDiagnostics( BootstrapState state, Guid? runtimeId, int bootstrapCount,
		int ownerCount, string ownerConfigurationPath, string message )
	{
		State = state;
		RuntimeId = runtimeId;
		BootstrapCount = bootstrapCount;
		OwnerCount = ownerCount;
		OwnerConfigurationPath = ownerConfigurationPath;
		Message = message;
	}
}
