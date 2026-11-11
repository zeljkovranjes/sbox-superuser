namespace SboxSuperuser.Engine.Messaging;

/// <summary>Delivers staff messages to connected players. EngineMessenger is the live adapter; tests record instead.</summary>
internal interface IPlayerMessenger
{
	/// <summary>Steam IDs of every connected player.</summary>
	IReadOnlyList<string> Online { get; }

	/// <summary>Sends one player a private chat line; false when they are not connected.</summary>
	bool SendPrivate( string steamId, string text );

	/// <summary>Adds a plain line to everyone's chat.</summary>
	void SendChat( string text );

	/// <summary>Shows text in the middle of everyone's screen for a few seconds.</summary>
	void SendCenter( string text );
}
