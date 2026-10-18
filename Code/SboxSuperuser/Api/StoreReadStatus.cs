namespace SboxSuperuser;

/// <summary>Host-local read outcome; missing data must never be confused with corruption.</summary>
public enum StoreReadStatus
{
	/// <summary>Unknown storage outcome.</summary>
	Unknown = 0,

	/// <summary>Found storage outcome.</summary>
	Found = 1,

	/// <summary>Missing storage outcome.</summary>
	Missing = 2,

	/// <summary>Corrupt storage outcome.</summary>
	Corrupt = 3,

	/// <summary>Unsupported Schema storage outcome.</summary>
	UnsupportedSchema = 4,

	/// <summary>Unavailable storage outcome.</summary>
	Unavailable = 5,

	/// <summary>Cancelled storage outcome.</summary>
	Cancelled = 6,

	/// <summary>Failed storage outcome.</summary>
	Failed = 7,
}
