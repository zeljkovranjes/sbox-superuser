using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Commands;

internal static class PlayerSelection
{
	internal static PlayerResolution Resolve( string text, IReadOnlyList<PlayerCandidate> onlinePlayers, bool requiresOnline )
	{
		if ( !string.IsNullOrEmpty( text ) && text.All( character => character is >= '0' and <= '9' ) )
		{
			try
			{
				SteamIdentity.Validate( text, nameof( text ) );
			}
			catch ( ArgumentException )
			{
				return Failed( "Use a nonzero canonical decimal Steam ID." );
			}

			return !requiresOnline || onlinePlayers.Any( player => player.SteamId == text )
				? new PlayerResolution( text, Array.Empty<PlayerCandidate>(), null )
				: Failed( "The specified Steam ID is not online." );
		}

		var matches = onlinePlayers.Where( player => string.Equals( player.DisplayName, text, StringComparison.OrdinalIgnoreCase ) )
			.OrderBy( player => player.SteamId, StringComparer.Ordinal ).ThenBy( player => player.DisplayName, StringComparer.Ordinal ).ToArray();
		return matches.Length switch
		{
			0 => Failed( "No online player has that exact display name; use a Steam ID for offline targets." ),
			1 => new PlayerResolution( matches[ 0 ].SteamId, Array.Empty<PlayerCandidate>(), null ),
			_ => new PlayerResolution( null, Array.AsReadOnly( matches.Take( 16 ).ToArray() ), "The player name is ambiguous; choose an explicit Steam ID." )
		};
	}

	private static PlayerResolution Failed( string error ) => new( null, Array.Empty<PlayerCandidate>(), error );
}
