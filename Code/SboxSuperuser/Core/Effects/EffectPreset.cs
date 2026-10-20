namespace SboxSuperuser.Core.Effects;

/// <summary>A ready-made effect script. Presets are read-only; templates are starting points for new scripts.</summary>
internal sealed record EffectPreset( string Id, string Title, string Description, string Source, string SuggestedTag = null );
