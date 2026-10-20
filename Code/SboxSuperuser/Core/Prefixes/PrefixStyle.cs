using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Core.Prefixes;

/// <summary>How a part is styled: a shipped preset (by ID), a script written in the menu, or a C# effect (by ID).</summary>
internal sealed record PrefixStyle( PrefixStyleKind Kind, string Value )
{
	internal const int MaximumIdLength = 64;

	internal static readonly PrefixStyle Plain = new( PrefixStyleKind.Preset, "white" );

	internal static PrefixStyle Create( PrefixStyleKind kind, string value )
	{
		value ??= "";
		switch ( kind )
		{
			case PrefixStyleKind.Script:
				if ( value.Length > EffectProgram.MaximumSourceLength )
					throw new ArgumentException( $"Effect scripts can be at most {EffectProgram.MaximumSourceLength} characters.", nameof( value ) );
				return new PrefixStyle( kind, value.Replace( "\r\n", "\n" ) );
			case PrefixStyleKind.Preset:
				if ( EffectPresets.Find( value ) is null ) throw new ArgumentException( $"There is no preset called '{value}'.", nameof( value ) );
				return new PrefixStyle( kind, value );
			case PrefixStyleKind.Code:
				if ( value.Length > MaximumIdLength ) throw new ArgumentException( "C# effect IDs can be at most 64 characters.", nameof( value ) );
				Contracts.ContractGuard.Key( value, nameof( value ) );
				return new PrefixStyle( kind, value );
			default:
				throw new ArgumentException( "Unknown style kind.", nameof( kind ) );
		}
	}

	/// <summary>The script that draws this style, or null for a C# effect.</summary>
	internal string Source => Kind switch
	{
		PrefixStyleKind.Script => Value,
		PrefixStyleKind.Preset => EffectPresets.Find( Value )?.Source ?? "white",
		_ => null
	};
}
