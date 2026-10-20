namespace SboxSuperuser;

/// <summary>One piece of a module's menu window, as sent to the viewer.</summary>
public sealed class AdminMenuElementRow
{
	/// <summary>What kind of element this is.</summary>
	public AdminMenuElementKind Kind { get; init; }

	/// <summary>The heading, paragraph or button label.</summary>
	public string Text { get; init; } = "";

	/// <summary>The command a button, form or live element uses; empty for the others.</summary>
	public string Command { get; init; } = "";

	/// <summary>Fixed arguments for a button or live element.</summary>
	public string[] Arguments { get; init; } = Array.Empty<string>();
}
