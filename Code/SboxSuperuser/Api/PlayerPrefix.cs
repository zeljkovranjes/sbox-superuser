namespace SboxSuperuser;

/// <summary>The prefix a player wears, as plain data. Use Superuser.RenderPrefix for the styled, animated form.</summary>
public sealed class PlayerPrefix
{
	internal PlayerPrefix( string id, string name, string tag, string template, bool isAnimated )
	{
		Id = id;
		Name = name;
		Tag = tag;
		Template = template;
		IsAnimated = isAnimated;
	}

	/// <summary>The prefix ID, such as "vip".</summary>
	public string Id { get; }

	/// <summary>The prefix's display name, such as "VIP".</summary>
	public string Name { get; }

	/// <summary>The tag text, such as "[VIP]".</summary>
	public string Tag { get; }

	/// <summary>Where the tag and name go, such as "%prefix% %name%".</summary>
	public string Template { get; }

	/// <summary>Whether it changes over time, so it needs redrawing every frame.</summary>
	public bool IsAnimated { get; }

	/// <summary>The template filled in with plain text, such as "[VIP] Alice".</summary>
	public string Format( string playerName ) => Core.Prefixes.PrefixTemplate.Format( Template, Tag, playerName ?? "" );
}
