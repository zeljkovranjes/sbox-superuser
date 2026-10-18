namespace SboxSuperuser;

/// <summary>Immutable host-local module state for operator diagnostics.</summary>
public sealed class ModuleStatus
{
	/// <summary>Gets the installed contract metadata.</summary>
	public ModuleDescriptor Descriptor { get; }
	/// <summary>Gets the observed lifecycle state.</summary>
	public ModuleState State { get; }
	/// <summary>Gets safe diagnostic text without exception details.</summary>
	public string Message { get; }

	internal ModuleStatus( ModuleDescriptor descriptor, ModuleState state, string message )
	{
		Descriptor = descriptor;
		State = state;
		Message = message;
	}
}
