using Sandbox.UI;

namespace SboxSuperuser.UI;

/// <summary>
/// A label that shows a player's name with their prefix and animates it. Set SteamId and PlayerName, from code
/// or Razor: &lt;PrefixLabel SteamId="@id" PlayerName="@name" /&gt;.
/// </summary>
public sealed class PrefixLabel : Label
{
	/// <summary>Creates the label; it shows nothing until PlayerName is set.</summary>
	public PrefixLabel()
	{
		IsRich = true;
		AddClass( "prefix-label" );
	}

	/// <summary>The player whose prefix to show.</summary>
	public string SteamId { get; set; } = "";

	/// <summary>The name drawn after the prefix.</summary>
	public string PlayerName { get; set; } = "";

	/// <summary>Redraws the markup only when the line actually changed this frame.</summary>
	public override void Tick()
	{
		base.Tick();
		var markup = Superuser.RenderPrefix( SteamId, PlayerName ).ToMarkup();
		if ( markup != Text ) Text = markup;
	}
}
