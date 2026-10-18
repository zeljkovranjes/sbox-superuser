namespace SboxSuperuser;

/// <summary>Host-local lifecycle state; quarantined modules cannot start another generation.</summary>
public enum ModuleState
{
	/// <summary>Installed with no active registrations.</summary>
	Disabled,
	/// <summary>Startup is staging registrations.</summary>
	Starting,
	/// <summary>Commands and subscriptions are active.</summary>
	Enabled,
	/// <summary>Entry points are removed and cleanup is running.</summary>
	Stopping,
	/// <summary>Startup failed and its contributions were removed.</summary>
	Faulted,
	/// <summary>Work exceeded the shutdown deadline or cleanup failed; no overlapping restart is allowed.</summary>
	Quarantined
}
