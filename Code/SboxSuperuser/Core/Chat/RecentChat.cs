namespace SboxSuperuser.Core.Chat;

/// <summary>
/// The last delivered public chat lines, kept in memory only for moderators. Nothing is written to disk; the
/// buffer is lost when the host runtime stops. Callers record only messages every player already saw.
/// </summary>
internal sealed class RecentChat
{
	internal const int Capacity = 100;
	private const int MaximumNameLength = 64;
	private const int MaximumMessageLength = 256;

	private readonly Queue<ChatLine> _lines = new();

	internal int Count => _lines.Count;

	internal void Record( string steamId, string name, string message, DateTimeOffset createdUtc )
	{
		if ( string.IsNullOrEmpty( steamId ) || string.IsNullOrWhiteSpace( message ) ) return;
		_lines.Enqueue( new ChatLine( steamId, Clean( name, MaximumNameLength ), Clean( message, MaximumMessageLength ), createdUtc ) );
		while ( _lines.Count > Capacity ) _lines.Dequeue();
	}

	/// <summary>Newest first; an empty Steam ID returns every player's lines.</summary>
	internal IReadOnlyList<ChatLine> Read( string steamId = "" ) => _lines
		.Where( line => steamId.Length == 0 || line.SteamId == steamId )
		.Reverse().ToArray();

	internal void Clear() => _lines.Clear();

	// Control characters could forge extra lines in a moderator's view, so they become spaces.
	private static string Clean( string text, int maximumLength )
	{
		var cleaned = new string( (text ?? "").Select( character => char.IsControl( character ) ? ' ' : character ).ToArray() ).Trim();
		return cleaned.Length <= maximumLength ? cleaned : cleaned.Substring( 0, maximumLength );
	}
}
