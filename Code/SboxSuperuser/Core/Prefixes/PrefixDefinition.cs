using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Core.Prefixes;

/// <summary>One prefix: its tag text, how the tag and the player's name are styled, and the line template.</summary>
internal sealed class PrefixDefinition
{
	internal const int MaximumIdLength = 32;
	internal const int MaximumNameLength = 48;
	internal const int MaximumTagLength = 32;
	internal const int MaximumPaletteColors = 16;

	private PrefixDefinition( string id, string name, string tag, string template, PrefixStyle tagStyle, PrefixStyle nameStyle, IReadOnlyList<string> palette )
	{
		Id = id;
		Name = name;
		Tag = tag;
		Template = template;
		TagStyle = tagStyle;
		NameStyle = nameStyle;
		Palette = palette;
		Pieces = PrefixTemplate.Split( template );
		PaletteValue = EffectValue.Of( (palette.Count == 0 ? EffectSymbols.DefaultPalette : palette.Select( Color ).ToArray())
			.Select( EffectValue.Of ).ToArray() );
	}

	internal string Id { get; }
	internal string Name { get; }
	internal string Tag { get; }
	internal string Template { get; }

	/// <summary>The template split into literal text, %prefix% and %name%.</summary>
	internal IReadOnlyList<string> Pieces { get; }
	internal PrefixStyle TagStyle { get; }
	internal PrefixStyle NameStyle { get; }

	/// <summary>Hex colors; empty means the WaW ^1 to ^5 palette.</summary>
	internal IReadOnlyList<string> Palette { get; }

	/// <summary>The palette as scripts see it.</summary>
	internal EffectValue PaletteValue { get; }

	internal static PrefixDefinition Create( string id, string name, string tag, string template = null,
		PrefixStyle tagStyle = null, PrefixStyle nameStyle = null, IEnumerable<string> palette = null )
	{
		ContractGuard.Key( id, nameof( id ) );
		if ( id.Length > MaximumIdLength ) throw new ArgumentException( $"Prefix IDs can be at most {MaximumIdLength} characters.", nameof( id ) );
		name = string.IsNullOrWhiteSpace( name ) ? id : name.Trim();
		if ( name.Length > MaximumNameLength || name.Any( char.IsControl ) )
			throw new ArgumentException( $"Prefix names are one line of at most {MaximumNameLength} characters.", nameof( name ) );
		tag ??= "";
		if ( tag.Length > MaximumTagLength || tag.Any( char.IsControl ) )
			throw new ArgumentException( $"Tags are one line of at most {MaximumTagLength} characters.", nameof( tag ) );

		var colors = (palette ?? Array.Empty<string>()).ToArray();
		if ( colors.Length > MaximumPaletteColors || colors.Any( color => !EffectColor.TryParseHex( color, out _ ) ) )
			throw new ArgumentException( $"A palette holds at most {MaximumPaletteColors} hex colors like #ff3333.", nameof( palette ) );
		return new PrefixDefinition( id, name, tag, PrefixTemplate.Validate( template ), tagStyle ?? PrefixStyle.Plain,
			nameStyle ?? PrefixStyle.Plain, Array.AsReadOnly( colors.Select( Normalize ).ToArray() ) );
	}

	internal PrefixDefinition With( string name = null, string tag = null, string template = null,
		PrefixStyle tagStyle = null, PrefixStyle nameStyle = null, IEnumerable<string> palette = null ) =>
		Create( Id, name ?? Name, tag ?? Tag, template ?? Template, tagStyle ?? TagStyle, nameStyle ?? NameStyle, palette ?? Palette );

	/// <summary>The line in plain text, for logs and screen readers.</summary>
	internal string Format( string playerName ) => PrefixTemplate.Format( Template, Tag, playerName ?? "" );

	private static EffectColor Color( string hex ) => EffectColor.TryParseHex( hex, out var color ) ? color : EffectColor.White;

	private static string Normalize( string hex ) => Color( hex ).ToHex().ToLowerInvariant();
}
