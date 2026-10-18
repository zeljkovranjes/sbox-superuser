namespace SboxSuperuser.Core.Menu;

/// <summary>Short, human wording for how long a restriction has left, such as "2h 14m".</summary>
internal static class TimeRemaining
{
	internal static string Format( TimeSpan remaining )
	{
		if ( remaining <= TimeSpan.Zero ) return "expiring";
		if ( remaining < TimeSpan.FromMinutes( 1 ) ) return "under 1m";
		if ( remaining < TimeSpan.FromHours( 1 ) ) return $"{(int)remaining.TotalMinutes}m";
		if ( remaining < TimeSpan.FromDays( 1 ) ) return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
		return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
	}
}
