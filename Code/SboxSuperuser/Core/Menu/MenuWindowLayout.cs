namespace SboxSuperuser.Core.Menu;

/// <summary>One admin menu window: whether it is open or pinned, and where it was last placed.</summary>
internal sealed class MenuWindowLayout
{
	internal bool Open { get; set; }
	internal bool Pinned { get; set; }

	/// <summary>False until the window has been placed once; the renderer then uses its default position.</summary>
	internal bool HasBounds { get; set; }
	internal float X { get; set; }
	internal float Y { get; set; }
	internal float Width { get; set; }
	internal float Height { get; set; }
}
