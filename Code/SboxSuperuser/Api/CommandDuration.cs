namespace SboxSuperuser;

/// <summary>An explicit positive duration or permanent value, without parsing or expiry scheduling.</summary>
public sealed class CommandDuration
{
	/// <summary>Gets the finite duration, or null for an explicit permanent value.</summary>
	public TimeSpan? Value { get; }

	/// <summary>Gets whether the caller explicitly selected permanent duration.</summary>
	public bool IsPermanent => !Value.HasValue;

	/// <summary>Gets the explicit permanent value; zero is never used as a synonym.</summary>
	public static CommandDuration Permanent { get; } = new CommandDuration();

	private CommandDuration()
	{
	}

	/// <summary>Creates a positive finite duration.</summary>
	public CommandDuration( TimeSpan value )
	{
		if ( value <= TimeSpan.Zero )
		{
			throw new ArgumentOutOfRangeException( nameof( value ), "Duration must be positive." );
		}

		Value = value;
	}
}
