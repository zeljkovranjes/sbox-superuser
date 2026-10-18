namespace SboxSuperuser;

/// <summary>Bounded, permission-filtered administration views.</summary>
public enum AdminQueryKind
{
	/// <summary>Search the live player list.</summary>
	Players,
	/// <summary>Inspect an exact online or offline Steam ID.</summary>
	Player,
	/// <summary>Read recent activity visible to the caller.</summary>
	Activity
}
