namespace SboxSuperuser.Core.Chat;

/// <summary>
/// Messages this player sent, for Up and Down in the chat input. Up walks back from the newest, Down walks forward
/// and past the newest returns to what was being typed. Repeating the last message doesn't add it again.
/// </summary>
internal sealed class SentHistory
{
	internal const int Capacity = 20;

	private readonly List<string> _sent = new();
	private int _position;
	private string _draft = "";

	/// <summary>Remembers a sent message and starts browsing from the newest again.</summary>
	internal void Add( string message )
	{
		if ( string.IsNullOrWhiteSpace( message ) ) return;
		if ( _sent.Count == 0 || _sent[ ^1 ] != message ) _sent.Add( message );
		if ( _sent.Count > Capacity ) _sent.RemoveAt( 0 );
		Reset();
	}

	/// <summary>Called when the input opens, so browsing always starts at the newest message.</summary>
	internal void Reset()
	{
		_position = _sent.Count;
		_draft = "";
	}

	/// <summary>The message before the current one, or null at the oldest. Keeps what is typed to come back to it.</summary>
	internal string Older( string typed )
	{
		if ( _position == 0 ) return null;
		if ( _position == _sent.Count ) _draft = typed ?? "";
		return _sent[ --_position ];
	}

	/// <summary>The message after the current one; past the newest it returns what was being typed. Null when not browsing.</summary>
	internal string Newer()
	{
		if ( _position >= _sent.Count ) return null;
		_position++;
		return _position == _sent.Count ? _draft : _sent[ _position ];
	}
}
