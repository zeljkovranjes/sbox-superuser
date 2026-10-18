using SboxSuperuser.Core.Commands;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser;

/// <summary>One online player in a host-supplied directory snapshot; it is not an authentication credential.</summary>
public sealed class CommandPlayer
{
	internal PlayerCandidate Candidate { get; }

	/// <summary>Gets the canonical decimal identity.</summary>
	public string SteamId => Candidate.SteamId;

	/// <summary>Gets the exact display name used for ambiguity checks.</summary>
	public string DisplayName => Candidate.DisplayName;

	/// <summary>Creates a bounded player-directory entry for parsing.</summary>
	public CommandPlayer( string steamId, string displayName )
	{
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		if ( string.IsNullOrWhiteSpace( displayName ) || displayName.Length > 256 || displayName.Any( char.IsControl ) )
		{
			throw new ArgumentException( "Display names must contain 1–256 non-control characters.", nameof( displayName ) );
		}

		Candidate = new PlayerCandidate( steamId, displayName );
	}
}
