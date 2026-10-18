namespace SboxSuperuser;

/// <summary>Declares target rechecks; it never resolves or authorizes a target.</summary>
public enum CommandTargetPolicy
{
	/// <summary>None argument policy.</summary>
	None = 0,

	/// <summary>Online Player argument policy.</summary>
	OnlinePlayer = 1,

	/// <summary>Any Player argument policy.</summary>
	AnyPlayer = 2,

	/// <summary>Punitive Online Player argument policy.</summary>
	PunitiveOnlinePlayer = 3,

	/// <summary>Punitive Any Player argument policy.</summary>
	PunitiveAnyPlayer = 4,
}
