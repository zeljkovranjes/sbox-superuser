namespace SboxSuperuser;

/// <summary>What one piece of a module's admin menu window is.</summary>
public enum AdminMenuElementKind
{
	/// <summary>A section title.</summary>
	Heading = 0,

	/// <summary>A paragraph of text.</summary>
	Text = 1,

	/// <summary>A dividing line.</summary>
	Separator = 2,

	/// <summary>A button that runs a command with fixed arguments.</summary>
	Button = 3,

	/// <summary>The command's generated form, the same one the Commands window shows.</summary>
	Form = 4,

	/// <summary>The reply of a read-only command, refreshed every few seconds while shown and right after a change from the menu.</summary>
	Live = 5
}
