namespace SboxSuperuser;

/// <summary>The letter a C# tag effect is styling, with the same inputs effect scripts get.</summary>
public readonly struct TagLetter
{
	/// <summary>Creates the input for one letter.</summary>
	public TagLetter( string text, int index, int count, string character, string part, double time, IReadOnlyList<Color> palette, int seed )
	{
		Text = text;
		Index = index;
		Count = count;
		Character = character;
		Part = part;
		Time = time;
		Palette = palette;
		Seed = seed;
	}

	/// <summary>The whole text of the part being drawn.</summary>
	public string Text { get; }

	/// <summary>This letter's position, from 0.</summary>
	public int Index { get; }

	/// <summary>How many letters the part has.</summary>
	public int Count { get; }

	/// <summary>This letter.</summary>
	public string Character { get; }

	/// <summary>"tag" or "name".</summary>
	public string Part { get; }

	/// <summary>UTC time in milliseconds, the same on every machine.</summary>
	public double Time { get; }

	/// <summary>The prefix's palette.</summary>
	public IReadOnlyList<Color> Palette { get; }

	/// <summary>A number fixed per player.</summary>
	public int Seed { get; }
}
