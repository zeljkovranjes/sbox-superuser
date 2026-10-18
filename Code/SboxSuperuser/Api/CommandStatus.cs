namespace SboxSuperuser;

/// <summary>Explicit execution outcome; an uninitialized or interrupted outcome is never success.</summary>
public enum CommandStatus
{
	/// <summary>Unknown outcome.</summary>
	Unknown = 0,

	/// <summary>Success outcome.</summary>
	Success = 1,

	/// <summary>No Change outcome.</summary>
	NoChange = 2,

	/// <summary>Invalid Arguments outcome.</summary>
	InvalidArguments = 3,

	/// <summary>Denied outcome.</summary>
	Denied = 4,

	/// <summary>Not Found outcome.</summary>
	NotFound = 5,

	/// <summary>Unsupported outcome.</summary>
	Unsupported = 6,

	/// <summary>Cancelled outcome.</summary>
	Cancelled = 7,

	/// <summary>Failed outcome.</summary>
	Failed = 8,

	/// <summary>Conflict outcome.</summary>
	Conflict = 9,

	/// <summary>Storage Unavailable outcome.</summary>
	StorageUnavailable = 10,
}
