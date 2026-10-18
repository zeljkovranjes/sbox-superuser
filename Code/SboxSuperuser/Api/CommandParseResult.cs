namespace SboxSuperuser;

/// <summary>Immutable argument conversion result; success neither authenticates nor authorizes execution.</summary>
public sealed class CommandParseResult
{
	/// <summary>Gets whether all arguments were converted successfully.</summary>
	public bool IsSuccess => Arguments is not null;

	/// <summary>Gets parsed values only on full success; failure never exposes partially parsed arguments.</summary>
	public CommandArguments Arguments { get; }

	/// <summary>Gets the safe conversion error, or null on success.</summary>
	public string Error { get; }

	/// <summary>Gets at most 16 stable choices for an ambiguous player name; none is selected.</summary>
	public IReadOnlyList<CommandPlayer> PlayerChoices { get; }

	internal CommandParseResult( CommandArguments arguments, string error, IEnumerable<CommandPlayer> choices = null )
	{
		Arguments = arguments;
		Error = error;
		PlayerChoices = Array.AsReadOnly( (choices ?? Array.Empty<CommandPlayer>()).ToArray() );
	}
}
