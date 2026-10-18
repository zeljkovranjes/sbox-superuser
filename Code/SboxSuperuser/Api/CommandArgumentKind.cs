namespace SboxSuperuser;

/// <summary>Supported argument shapes; parsing and conversion are implemented in T08.</summary>
public enum CommandArgumentKind
{
	/// <summary>String argument policy.</summary>
	String = 0,

	/// <summary>Integer argument policy.</summary>
	Integer = 1,

	/// <summary>Number argument policy.</summary>
	Number = 2,

	/// <summary>Boolean argument policy.</summary>
	Boolean = 3,

	/// <summary>Enum argument policy.</summary>
	Enum = 4,

	/// <summary>Duration argument policy.</summary>
	Duration = 5,

	/// <summary>Player argument policy.</summary>
	Player = 6,
}
