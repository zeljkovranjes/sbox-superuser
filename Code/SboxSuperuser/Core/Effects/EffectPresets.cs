namespace SboxSuperuser.Core.Effects;

/// <summary>
/// The shipped effects. The animated ones port the World at War clan-tag effects from
/// github.com/zeljkovranjes/waw-color-tags with the same timings; the palette defaults to WaW's ^1 to ^5.
/// </summary>
internal static class EffectPresets
{
	internal static readonly IReadOnlyList<EffectPreset> All = new EffectPreset[]
	{
		new( "rain", "Rain", "Every letter a different palette color, scrolling one step every 100 ms.", """
			// rain: each letter takes the next palette color, and the colors scroll every 100 ms.
			color = palette[floor(t / 100) + i]
			""", "rain" ),
		new( "cycl", "Cycle", "A new palette color wipes across the text, one letter every 100 ms.", """
			// cycl: a new color wipes across the text one letter at a time, then the next color follows.
			step = floor(t / 100)
			base = floor(step / L)
			edge = step % L
			color = palette[i < edge ? base : base - 1]
			""", "cycl" ),
		new( "cyln", "Scanner", "A red eye sweeps back and forth, slowing at each end (WaW cyln and krdr).", """
			// cyln / krdr: one red letter scans back and forth, easing at each end.
			eye = floor((cos(t * 0.004) + 1) * 0.5 * L)
			color = i == eye ? red : white
			""", "cyln" ),
		new( "move", "Slide", "The text slides sideways and bounces back.", """
			// move: slides the text right and back. WaW slides inside a 40-letter field; lower it for short lines.
			field = 40
			offset = floor((1 - abs(sin(t * 0.004))) * max(0, field - L))
			""", "move" ),
		new( "dots", "Bouncing dot", "One dot becomes an o and bounces between the dots every 75 ms. Use a tag of dots.", """
			// ....: one dot turns into an o and bounces between the dots every 75 ms.
			if ch == "." {
				char = i == pingpong(t / 75, L) ? "o" : "."
			}
			""", "...." ),
		new( "stars", "Bouncing plus", "Stars become dashes with a + bouncing between them every 75 ms. Use a tag of stars.", """
			// ****: the stars become dashes, with a + bouncing between them every 75 ms.
			if ch == "*" {
				char = i == pingpong(t / 75, L) ? "+" : "-"
			}
			""", "****" ),
		new( "red", "Red", "Solid red, WaW's ^1 (RED).", "// RED: solid ^1.\nred", "RED" ),
		new( "green", "Green", "Solid green, WaW's ^2 (GRN).", "// GRN: solid ^2.\ngreen", "GRN" ),
		new( "yellow", "Yellow", "Solid yellow, WaW's ^3 (YELW).", "// YELW: solid ^3.\nyellow", "YELW" ),
		new( "blue", "Blue", "Solid blue, WaW's ^4 (BLUE).", "// BLUE: solid ^4.\nblue", "BLUE" ),
		new( "cyan", "Cyan", "Solid cyan, WaW's ^5 (CYAN).", "// CYAN: solid ^5.\ncyan", "CYAN" ),
		new( "pink", "Pink", "Solid pink, WaW's ^6, used for any other tag.", "// Any other WaW tag: solid ^6.\npink" ),
		new( "white", "Plain", "Plain white text.", "// Plain white, WaW's ^7.\nwhite" )
	};

	internal static readonly IReadOnlyList<EffectPreset> Templates = new EffectPreset[]
	{
		new( "solid", "Solid color", "One color for every letter.", """
			// One color for every letter. Type a color name (red, cyan, pink ...),
			// or a hex color like #ff8800. The color picker inserts one for you.
			#ff8800
			""" ),
		new( "gradient", "Gradient", "Blends from the first palette color to the second.", """
			// Blends from the first palette color to the second, left to right.
			color = mix(palette[0], palette[1], i / max(1, L - 1))
			""" ),
		new( "rainbow", "Rainbow scroll", "A smooth rainbow that scrolls along the text.", """
			// A smooth rainbow. Raise speed to scroll faster, spread to squeeze the colors.
			speed = 0.2
			spread = 30
			color = hsv(t * speed + i * spread, 1, 1)
			""" ),
		new( "wipe", "Wipe", "Palette colors wipe across the text one after another.", """
			// Each palette color wipes across the text, one letter every step.
			step = floor(t / 100)
			edge = step % L
			color = palette[i < edge ? floor(step / L) : floor(step / L) - 1]
			""" ),
		new( "scanner", "Scanner", "One highlighted letter sweeps back and forth.", """
			// One letter in the first palette color sweeps back and forth.
			eye = floor((cos(t * 0.004) + 1) * 0.5 * L)
			if i == eye {
				color = palette[0]
				bold = true
			}
			""" ),
		new( "blink", "Blink", "Fades the text in and out.", """
			// Fades the text in and out once a second. The second number is the length in ms.
			color = palette[0]
			alpha = 0.25 + 0.75 * wave(t, 1000)
			""" ),
		new( "typewriter", "Typewriter", "Types the text out one letter at a time, then starts again.", """
			// Types one letter every 120 ms, waits a moment, then starts over.
			typed = floor(t / 120) % (L + 8)
			hidden = i >= typed
			""" )
	};

	internal static EffectPreset Find( string id ) => All.FirstOrDefault( preset => preset.Id == id );
}
