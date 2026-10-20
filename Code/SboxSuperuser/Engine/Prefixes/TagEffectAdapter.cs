using Sandbox;
using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Engine.Prefixes;

/// <summary>Runs a C# <see cref="ITagEffect"/> as a glyph effect. An exception fails the frame, like a broken script.</summary>
internal sealed class TagEffectAdapter : IGlyphEffect
{
	private readonly ITagEffect _effect;
	private EffectValue _paletteSource;
	private Color[] _palette = Array.Empty<Color>();
	private bool _reported;

	internal TagEffectAdapter( ITagEffect effect ) => _effect = effect;

	public bool IsAnimated => _effect.IsAnimated;

	public bool Render( in EffectInput input, List<EffectGlyph> output )
	{
		var text = input.Text ?? "";
		if ( !ReferenceEquals( _paletteSource.Items, input.Palette.Items ) )
		{
			_paletteSource = input.Palette;
			_palette = (input.Palette.Items ?? Array.Empty<EffectValue>()).Select( item => EffectColors.ToColor( item.Color ) ).ToArray();
		}

		var first = output.Count;
		try
		{
			for ( var index = 0; index < text.Length; index++ )
			{
				var character = text[ index ].ToString();
				var style = _effect.Style( new TagLetter( text, index, text.Length, character, input.Part, input.Time, _palette, input.Seed ) );
				if ( style.Hidden ) continue;
				var color = EffectColors.FromColor( style.Color );
				output.Add( new EffectGlyph( style.Character ?? character, color.WithAlpha( color.A * Math.Clamp( style.Alpha, 0, 1 ) ),
					Math.Clamp( style.Offset, -64, 64 ), style.Bold, style.Italic ) );
			}
			return true;
		}
		catch ( Exception error )
		{
			output.RemoveRange( first, output.Count - first );
			if ( !_reported ) Log.Warning( $"[Superuser] Tag effect '{_effect.Id}' failed; drawing plain. type={error.GetType().Name}." );
			_reported = true;
			return false;
		}
	}
}
