using Sandbox;

namespace SboxSuperuser.UI;

/// <summary>One colored piece of a line. Swatch marks colors, which get a small sample drawn under them.</summary>
internal readonly record struct HighlightSpan( int Line, int Column, int Length, Vector4 Color, Vector4? Swatch );
