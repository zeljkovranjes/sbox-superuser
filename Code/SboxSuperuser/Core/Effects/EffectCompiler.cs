namespace SboxSuperuser.Core.Effects;

/// <summary>Compiles effect scripts: tokens, then statements, then checks. Never throws, whatever the input.</summary>
internal static class EffectCompiler
{
	internal static EffectCompilation Compile( string source )
	{
		source = (source ?? "").Replace( "\r\n", "\n" );
		var diagnostics = new List<EffectDiagnostic>();
		var tokens = EffectLexer.Tokenize( source, diagnostics );
		if ( source.Length > EffectProgram.MaximumSourceLength )
		{
			diagnostics.Add( new EffectDiagnostic( $"an effect can be at most {EffectProgram.MaximumSourceLength} characters long",
				EffectProgram.MaximumSourceLength, source.Length - EffectProgram.MaximumSourceLength ) );
			return new EffectCompilation( source, tokens, diagnostics, null );
		}

		var statements = EffectParser.Parse( source, tokens, diagnostics );
		var checker = EffectChecker.Check( statements, diagnostics );
		var ordered = diagnostics.OrderBy( diagnostic => diagnostic.Start ).ToArray();
		var program = ordered.Length == 0 ? new EffectProgram( source, statements, checker.SlotCount, checker.UsesTime ) : null;
		return new EffectCompilation( source, tokens, ordered, program );
	}
}
