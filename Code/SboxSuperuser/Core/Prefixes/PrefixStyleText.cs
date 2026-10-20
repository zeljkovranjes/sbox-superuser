using System.Text;
using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Core.Prefixes;

/// <summary>
/// The text form of a style in commands: "preset:rain", "code:game.fire", a one-line script typed in the console,
/// or "b64:" plus a base64 script, which is how the menu sends multi-line scripts through 256-character arguments.
/// </summary>
internal static class PrefixStyleText
{
	internal const int ChunkLength = 256;
	internal const int MaximumChunks = 13;
	private const string Base64 = "b64:";

	internal static PrefixStyle Parse( string text )
	{
		text = (text ?? "").Trim();
		if ( text.StartsWith( "preset:", StringComparison.Ordinal ) ) return PrefixStyle.Create( PrefixStyleKind.Preset, text.Substring( 7 ) );
		if ( text.StartsWith( "code:", StringComparison.Ordinal ) ) return PrefixStyle.Create( PrefixStyleKind.Code, text.Substring( 5 ) );
		if ( !text.StartsWith( Base64, StringComparison.Ordinal ) ) return PrefixStyle.Create( PrefixStyleKind.Script, text );
		try
		{
			// Arguments are joined with spaces, which base64 never contains, so dropping them restores the payload.
			var payload = text.Substring( Base64.Length ).Replace( " ", "" );
			return PrefixStyle.Create( PrefixStyleKind.Script, Encoding.UTF8.GetString( Convert.FromBase64String( payload ) ) );
		}
		catch ( FormatException )
		{
			throw new ArgumentException( "The encoded effect script is damaged.", nameof( text ) );
		}
	}

	/// <summary>Command arguments that carry a style; throws when a script is too long to send.</summary>
	internal static IReadOnlyList<string> Arguments( PrefixStyle style )
	{
		switch ( style.Kind )
		{
			case PrefixStyleKind.Preset: return new[] { "preset:" + style.Value };
			case PrefixStyleKind.Code: return new[] { "code:" + style.Value };
		}
		if ( style.Value.Length > EffectProgram.MaximumSourceLength )
			throw new ArgumentException( $"Effect scripts can be at most {EffectProgram.MaximumSourceLength} characters." );
		var encoded = Base64 + Convert.ToBase64String( Encoding.UTF8.GetBytes( style.Value ) );
		var chunks = Enumerable.Range( 0, (encoded.Length + ChunkLength - 1) / ChunkLength )
			.Select( index => encoded.Substring( index * ChunkLength, Math.Min( ChunkLength, encoded.Length - index * ChunkLength ) ) ).ToArray();
		if ( chunks.Length > MaximumChunks ) throw new ArgumentException( "This effect is too long to send; shorten it or use fewer special characters." );
		return chunks;
	}
}
