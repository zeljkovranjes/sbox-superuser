namespace SboxSuperuser.Core.Effects;

/// <summary>
/// One parsed piece of a script. A single node shape keeps the parser, checker and interpreter small:
/// <see cref="Children"/> holds operands, arguments or list items, <see cref="Body"/> and <see cref="Else"/> hold
/// statements, and <see cref="Slot"/> and <see cref="Arguments"/> are filled in by the checker.
/// </summary>
internal sealed class EffectNode
{
	internal EffectNodeKind Kind { get; init; }
	internal string Name { get; init; } = "";
	internal EffectValue Value { get; init; }
	internal EffectNode[] Children { get; init; } = Array.Empty<EffectNode>();
	internal EffectNode[] Body { get; init; } = Array.Empty<EffectNode>();
	internal EffectNode[] Else { get; init; } = Array.Empty<EffectNode>();
	internal int Start { get; init; }
	internal int Length { get; init; }
	internal int Slot { get; set; } = -1;

	// Argument space for a call. The language has no recursion, so a call node is never re-entered mid-evaluation.
	internal EffectValue[] Arguments { get; set; }
}
