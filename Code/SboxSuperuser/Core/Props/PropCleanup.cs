namespace SboxSuperuser.Core.Props;

/// <summary>
/// How long a player's props stay after they leave, before Superuser removes them; null keeps them until someone cleans
/// up. Coming back before then cancels the removal.
/// </summary>
internal sealed record PropCleanup( TimeSpan? Delay )
{
	internal static readonly TimeSpan Minimum = TimeSpan.FromSeconds( 10 );
	internal static readonly TimeSpan Maximum = TimeSpan.FromDays( 7 );
	internal static PropCleanup Default { get; } = new( TimeSpan.FromMinutes( 2 ) );

	/// <summary>Why this delay can't be used, or null when it can.</summary>
	internal static string Problem( TimeSpan? delay ) =>
		delay is { } value && (value < Minimum || value > Maximum) ? "The delay is between 10 seconds and 7 days, or permanent to keep props." : null;
}
