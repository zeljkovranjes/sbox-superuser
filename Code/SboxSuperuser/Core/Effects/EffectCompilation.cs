namespace SboxSuperuser.Core.Effects;

/// <summary>The result of compiling a script: tokens for coloring, problems to show, and a program when there are none.</summary>
internal sealed record EffectCompilation( string Source, IReadOnlyList<EffectToken> Tokens, IReadOnlyList<EffectDiagnostic> Diagnostics, EffectProgram Program )
{
	internal bool Succeeded => Program is not null;
}
