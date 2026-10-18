namespace SboxSuperuser;

/// <summary>Recoverable write outcome; only Committed reports a confirmed persisted revision.</summary>
public enum StoreWriteStatus
{
	/// <summary>Unknown storage outcome.</summary>
	Unknown = 0,

	/// <summary>Committed storage outcome.</summary>
	Committed = 1,

	/// <summary>Conflict storage outcome.</summary>
	Conflict = 2,

	/// <summary>Unsupported Schema storage outcome.</summary>
	UnsupportedSchema = 3,

	/// <summary>Unavailable storage outcome.</summary>
	Unavailable = 4,

	/// <summary>Cancelled storage outcome.</summary>
	Cancelled = 5,

	/// <summary>Failed storage outcome.</summary>
	Failed = 6,

	/// <summary>Candidate data violates the supported transactional schema.</summary>
	InvalidData = 7,

	/// <summary>Existing committed data is corrupt; explicit operator recovery is required.</summary>
	Corrupt = 8,
}
