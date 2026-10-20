using System.Text;

namespace SboxSuperuser.Core.Effects;

/// <summary>Builds the effect-script reference pages from the same tables the checker uses, so they never drift.</summary>
internal static class EffectReference
{
	internal static string Markdown()
	{
		var text = new StringBuilder();
		text.AppendLine( "# Effect reference" );
		text.AppendLine();
		text.AppendLine( "An effect runs once for every letter, every frame. It reads the inputs, sets any outputs it wants, and the letter is drawn with them. Outputs you don't set keep their starting value." );
		Table( text, "Inputs", "Read these; they change per letter or per frame.", EffectSymbolKind.Input );
		Table( text, "Outputs", "Set these to style the letter.", EffectSymbolKind.Output );
		Table( text, "Placeholders", "Text you can use in expressions.", EffectSymbolKind.Placeholder );
		Table( text, "Functions", "Call them with values in brackets, separated by commas.", EffectSymbolKind.Function );

		text.AppendLine();
		text.AppendLine( "## Colors" );
		text.AppendLine();
		text.AppendLine( "Write a name, a hex color (`#f80`, `#ff8800` or `#ff8800cc` with alpha), `rgb(255, 136, 0)` or `hsv(30, 1, 1)`. The first eight names are the World at War palette `^0` to `^7`." );
		text.AppendLine();
		text.AppendLine( "| Name | Hex |" );
		text.AppendLine( "|---|---|" );
		foreach ( var (name, color) in EffectSymbols.Colors ) text.AppendLine( $"| `{name}` | `{color.ToHex().ToLowerInvariant()}` |" );

		text.AppendLine();
		text.AppendLine( "## Rules" );
		text.AppendLine();
		text.AppendLine( "- One statement per line, or separate them with `;`. `//` starts a comment, `/* */` wraps one." );
		text.AppendLine( "- A line that is only a color, like `red`, sets `color`." );
		text.AppendLine( "- `name = value` stores a value. New names are your own variables; they start at 0 for every letter." );
		text.AppendLine( "- `if condition { ... } else if condition { ... } else { ... }`, and `condition ? a : b` for a choice in one value." );
		text.AppendLine( "- Operators: `+ - * / %`, `== != < <= > >=`, `and` (`&&`), `or` (`||`), `not` (`!`). `+` joins text." );
		text.AppendLine( "- Lists use `[a, b, c]`. `list[n]` and `text[n]` wrap around, so `palette[i]` never runs off the end." );
		text.AppendLine( "- Dividing by zero gives 0. There are no loops; an effect can be at most 2048 characters." );
		text.AppendLine( "- If an effect fails while running, that part is drawn plain white for the frame; the editor shows why." );
		return text.ToString();
	}

	internal static string PresetsMarkdown()
	{
		var text = new StringBuilder();
		text.AppendLine( "# Presets" );
		text.AppendLine();
		text.AppendLine( "Presets ship with Superuser and can't be edited; **Duplicate** in the effect editor makes an editable copy. The animated ones port the World at War clan-tag effects with the same timings, using the prefix palette (WaW's `^1` to `^5` unless you set one)." );
		foreach ( var preset in EffectPresets.All )
		{
			text.AppendLine();
			text.AppendLine( $"## {preset.Title} (`preset:{preset.Id}`)" );
			text.AppendLine();
			text.AppendLine( preset.Description + (preset.SuggestedTag is null ? "" : $" Classic tag: `{preset.SuggestedTag}`.") );
			text.AppendLine();
			text.AppendLine( "```effect" );
			text.AppendLine( preset.Source.TrimEnd() );
			text.AppendLine( "```" );
		}
		return text.ToString();
	}

	private static void Table( StringBuilder text, string title, string intro, EffectSymbolKind kind )
	{
		text.AppendLine();
		text.AppendLine( $"## {title}" );
		text.AppendLine();
		text.AppendLine( intro );
		text.AppendLine();
		text.AppendLine( "| Name | What it is | Example |" );
		text.AppendLine( "|---|---|---|" );
		foreach ( var symbol in EffectSymbols.All.Where( symbol => symbol.Kind == kind ) )
		{
			var name = kind == EffectSymbolKind.Function ? $"{symbol.Name}()" : symbol.Name;
			text.AppendLine( $"| `{name}` | {symbol.Help} | `{symbol.Example}` |" );
		}
	}
}
