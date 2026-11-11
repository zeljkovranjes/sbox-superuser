namespace SboxSuperuser;

/// <summary>One banned map, addon or entity in the Content bans window.</summary>
public sealed class AdminContentBanRow
{
	/// <summary>Gets what is banned: map, addon or entity.</summary>
	public string Kind { get; init; } = "";

	/// <summary>Gets the ident, path or type name, where * matches anything.</summary>
	public string Pattern { get; init; } = "";

	/// <summary>Gets the name to show, such as a map's title; the pattern when it can't be looked up.</summary>
	public string Title { get; init; } = "";

	/// <summary>Gets the icon as a texture path or URL; empty for patterns and anything that can't be looked up.</summary>
	public string Icon { get; init; } = "";

	/// <summary>Gets why, shown to players when it is refused; empty when none was given.</summary>
	public string Reason { get; init; } = "";
}
