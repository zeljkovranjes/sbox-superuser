namespace SboxSuperuser.Core.Menu;

/// <summary>
/// Short-lived pop-up notices, newest last. Each shows for a few seconds and fades out over its last second;
/// only the newest few are kept so a burst of results cannot fill the screen.
/// </summary>
internal sealed class Notices<T>
{
	internal static readonly TimeSpan Lifetime = TimeSpan.FromSeconds( 5 );
	private static readonly TimeSpan FadeTime = TimeSpan.FromSeconds( 1 );
	internal const int MaximumShown = 4;

	private readonly List<(T Value, DateTimeOffset Shown)> _items = new();

	internal void Add( T value, DateTimeOffset now )
	{
		_items.Add( (value, now) );
		if ( _items.Count > MaximumShown ) _items.RemoveAt( 0 );
	}

	/// <summary>Drops expired notices and returns the rest with their opacity (1 = fully visible).</summary>
	internal IReadOnlyList<(T Value, float Opacity)> Visible( DateTimeOffset now )
	{
		_items.RemoveAll( item => now - item.Shown >= Lifetime );
		return _items.Select( item =>
		{
			var left = Lifetime - (now - item.Shown);
			var opacity = left >= FadeTime ? 1f : (float)(left / FadeTime);
			return (item.Value, opacity);
		} ).ToArray();
	}
}
