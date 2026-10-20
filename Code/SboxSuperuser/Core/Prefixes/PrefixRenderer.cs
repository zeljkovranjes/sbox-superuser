using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Core.Prefixes;

/// <summary>
/// Draws one prefixed name as styled runs: the template's literal text plain, the tag and the name through their
/// effects. A missing or failing effect draws its part plain white, so a broken script never hides a name.
/// </summary>
internal sealed class PrefixRenderer
{
	private readonly List<EffectGlyph> _glyphs = new();

	internal void Render( PrefixDefinition prefix, IGlyphEffect tagEffect, IGlyphEffect nameEffect,
		string name, double time, int seed, List<LineRun> runs )
	{
		name ??= "";
		foreach ( var piece in prefix.Pieces )
		{
			if ( piece == PrefixTemplate.Prefix ) RenderPart( prefix, prefix.Tag, "tag", tagEffect, name, time, seed, runs );
			else if ( piece == PrefixTemplate.Name ) RenderPart( prefix, name, "name", nameEffect, name, time, seed, runs );
			else Append( runs, new LineRun( piece, EffectColor.White, 0, false, false ) );
		}
	}

	private void RenderPart( PrefixDefinition prefix, string text, string part, IGlyphEffect effect,
		string name, double time, int seed, List<LineRun> runs )
	{
		if ( text.Length == 0 ) return;
		_glyphs.Clear();
		var input = new EffectInput( text, part, time, seed, prefix.PaletteValue, name, prefix.Tag );
		if ( effect is null || !effect.Render( input, _glyphs ) )
		{
			Append( runs, new LineRun( text, EffectColor.White, 0, false, false ) );
			return;
		}

		// An offset pushes its letter right and every later letter along with it, like typing spaces before it.
		var shift = 0f;
		foreach ( var glyph in _glyphs )
		{
			var target = MathF.Max( shift, glyph.Offset );
			Append( runs, new LineRun( glyph.Text, glyph.Color, target - shift, glyph.Bold, glyph.Italic ) );
			shift = target;
		}
	}

	// Neighbouring letters in the same style become one run, so drawing and markup stay small.
	private static void Append( List<LineRun> runs, LineRun run )
	{
		if ( run.Text.Length == 0 && run.Gap == 0 ) return;
		if ( runs.Count > 0 && run.Gap == 0 )
		{
			var last = runs[ ^1 ];
			if ( last.Color == run.Color && last.Bold == run.Bold && last.Italic == run.Italic )
			{
				runs[ ^1 ] = last with { Text = last.Text + run.Text };
				return;
			}
		}
		runs.Add( run );
	}
}
