namespace SboxSuperuser;

/// <summary>Observed bootstrap health; only explicit owner configuration grants owner membership.</summary>
public enum BootstrapState
{
	/// <summary>Disabled bootstrap state.</summary>
	Disabled = 0,

	/// <summary>Unsupported bootstrap state.</summary>
	Unsupported = 1,

	/// <summary>Duplicate Bootstrap bootstrap state.</summary>
	DuplicateBootstrap = 2,

	/// <summary>Ready bootstrap state.</summary>
	Ready = 3,

	/// <summary>Owners Missing bootstrap state.</summary>
	OwnersMissing = 4,

	/// <summary>Owners Invalid bootstrap state.</summary>
	OwnersInvalid = 5,

	/// <summary>Disposed bootstrap state.</summary>
	Disposed = 6,
}
