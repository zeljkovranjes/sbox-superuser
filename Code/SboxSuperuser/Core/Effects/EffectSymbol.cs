namespace SboxSuperuser.Core.Effects;

/// <summary>A built-in name scripts can use, with the help text the editor shows for it.</summary>
internal sealed record EffectSymbol( string Name, EffectSymbolKind Kind, string Help, string Example, int MinArguments = 0, int MaxArguments = 0 );
