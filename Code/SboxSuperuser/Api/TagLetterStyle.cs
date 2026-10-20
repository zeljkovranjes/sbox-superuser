namespace SboxSuperuser;

/// <summary>How a C# tag effect draws one letter. Start from <see cref="Of"/> so opacity begins at 1.</summary>
public struct TagLetterStyle
{
	/// <summary>A style with a color and everything else at its default.</summary>
	public static TagLetterStyle Of( Color color ) => new() { Color = color, Alpha = 1 };

	/// <summary>The letter's color.</summary>
	public Color Color { get; set; }

	/// <summary>Opacity from 0 to 1, multiplied with the color's alpha.</summary>
	public float Alpha { get; set; }

	/// <summary>Replacement text, or null to keep the letter.</summary>
	public string Character { get; set; }

	/// <summary>Pushes the letter, and every letter after it, right by this many letter widths.</summary>
	public float Offset { get; set; }

	/// <summary>Draws the letter bold.</summary>
	public bool Bold { get; set; }

	/// <summary>Draws the letter slanted.</summary>
	public bool Italic { get; set; }

	/// <summary>Leaves the letter out.</summary>
	public bool Hidden { get; set; }
}
