namespace SboxSuperuser;

/// <summary>One installed module in an About reply.</summary>
public sealed class AdminModuleRow
{
	/// <summary>Gets the module ID.</summary>
	public string Id { get; init; } = "";

	/// <summary>Gets the module API version it was built against.</summary>
	public int ApiVersion { get; init; }

	/// <summary>Gets the lifecycle state, such as Running or Failed.</summary>
	public string State { get; init; } = "";

	/// <summary>Gets safe diagnostic text without exception details.</summary>
	public string Message { get; init; } = "";
}
