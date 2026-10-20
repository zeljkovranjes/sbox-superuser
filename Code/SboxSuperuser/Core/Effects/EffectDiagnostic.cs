namespace SboxSuperuser.Core.Effects;

/// <summary>A problem in a script, with the exact characters to underline and a plain-English message.</summary>
internal sealed record EffectDiagnostic( string Message, int Start, int Length )
{
	/// <summary>The 1-based line the problem starts on.</summary>
	internal int Line( string source ) => source.Take( Math.Min( Start, source.Length ) ).Count( c => c == '\n' ) + 1;

	internal string Describe( string source ) => $"line {Line( source )}: {Message}";
}
