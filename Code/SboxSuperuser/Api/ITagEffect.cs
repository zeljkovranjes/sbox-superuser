namespace SboxSuperuser;

/// <summary>
/// A prefix effect written in C#, for styles effect scripts can't express. Register it on every machine with
/// Superuser.RegisterTagEffect; prefixes pick it as code:&lt;Id&gt;.
/// </summary>
public interface ITagEffect
{
	/// <summary>Lowercase dotted ID, such as "game.fire".</summary>
	string Id { get; }

	/// <summary>Name shown in the effect pickers.</summary>
	string Title { get; }

	/// <summary>False when the effect looks the same every frame.</summary>
	bool IsAnimated { get; }

	/// <summary>Styles one letter. Called for every letter, every frame, on the main thread; keep it cheap.</summary>
	TagLetterStyle Style( in TagLetter letter );
}
