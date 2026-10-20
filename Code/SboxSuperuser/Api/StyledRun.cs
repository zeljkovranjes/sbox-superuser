namespace SboxSuperuser;

/// <summary>A stretch of a styled line in one style.</summary>
public readonly struct StyledRun
{
	internal StyledRun( string text, Color color, float space, bool bold, bool italic )
	{
		Text = text;
		Color = color;
		Space = space;
		Bold = bold;
		Italic = italic;
	}

	/// <summary>The text to draw.</summary>
	public string Text { get; }

	/// <summary>Its color, alpha included.</summary>
	public Color Color { get; }

	/// <summary>Empty space before it, in letter widths.</summary>
	public float Space { get; }

	/// <summary>Whether it is bold.</summary>
	public bool Bold { get; }

	/// <summary>Whether it is slanted.</summary>
	public bool Italic { get; }
}
