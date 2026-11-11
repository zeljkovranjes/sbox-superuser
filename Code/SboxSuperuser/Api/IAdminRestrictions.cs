namespace SboxSuperuser;

/// <summary>Host-only module capability registered with RegisterService&lt;IAdminRestrictions&gt;; participates in player actions and active restrictions.</summary>
public interface IAdminRestrictions
{
	/// <summary>Declares up to 16 restriction types. Commands must belong to the registering module.</summary>
	IReadOnlyList<AdminRestrictionType> Types { get; }
	/// <summary>Returns up to 512 active records for one type; throw when storage is unavailable instead of reporting an empty list.</summary>
	IReadOnlyList<AdminModerationRow> ReadActive( string kind );
}

/// <summary>Names a module's restriction and existing permission-checked apply/restore commands.</summary>
public sealed record AdminRestrictionType( string Kind, string Label, string ApplyCommand, string RestoreCommand, string ReadPermission );

/// <summary>A host-filtered player action supplied by a module; executing it still rechecks authority.</summary>
public sealed class AdminPlayerActionRow
{
	public string Label { get; init; }
	public string Command { get; init; }
	public bool Restore { get; init; }
}
