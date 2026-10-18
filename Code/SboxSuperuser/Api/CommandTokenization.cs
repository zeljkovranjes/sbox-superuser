using SboxSuperuser.Core.Commands;

namespace SboxSuperuser;

/// <summary>Bounded tokenization result; malformed input returns no partial tokens.</summary>
public sealed class CommandTokenization
{
	/// <summary>Gets whether the complete input was accepted.</summary>
	public bool IsSuccess => Error is null;

	/// <summary>Gets immutable decoded tokens, including explicit empty quoted tokens.</summary>
	public IReadOnlyList<string> Tokens { get; }

	/// <summary>Gets a safe grammar error, or null on success.</summary>
	public string Error { get; }

	internal CommandTokenization( Tokenization result )
	{
		Tokens = result.Tokens;
		Error = result.Error;
	}
}
