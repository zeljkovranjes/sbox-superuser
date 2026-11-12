using Sandbox;

namespace SboxSuperuser.Engine.Bootstrap;

// What the runtime learns from each player's connection: users.json claims, how they connected, and voice.
internal sealed partial class BootstrapRuntime
{
	/// <summary>
	/// Reads the player's claims from the engine's config/users.json, such as admin or superuser. The host is skipped:
	/// the engine grants it every claim, which would make anyone hosting a listen server an owner.
	/// </summary>
	internal void ApplyEngineClaims( Connection connection )
	{
		if ( connection is null || connection.IsHost || !IsUsable ) return;
		var claims = Core.Permissions.ClaimMemberships.Grants.Keys.Where( connection.HasPermission ).ToArray();
		Policy.Claims.Set( connection.SteamId.ToString(), claims );
		if ( claims.Length > 0 ) Log.Info( $"[Superuser] {connection.DisplayName} has users.json claims: {string.Join( ", ", claims )}." );
	}

	/// <summary>Remembers how the player connected, for alt-account detection.</summary>
	internal void RecordConnection( Connection connection )
	{
		if ( connection is null || connection.IsHost || !IsUsable ) return;
		var party = connection.PartyId.ToString() is { } id && id != "0" ? id : "";
		Alts.Record( connection.SteamId.ToString(), connection.DisplayName, connection.OwnerSteamId.ToString(), connection.Address, party );
	}

	internal void ForgetEngineClaims( Connection connection )
	{
		if ( connection is not null ) Policy.Claims.Remove( connection.SteamId.ToString() );
	}

	private const double VoiceGagRefreshSeconds = 5;
	// Called every frame by the session; gags are only worked out twice a second.
	private const double VoiceGagCheckSeconds = 0.5;
	private string[] _publishedGags = Array.Empty<string>();
	private DateTimeOffset _nextGagPublish;
	private DateTimeOffset _nextGagCheck;

	/// <summary>
	/// Tells every player who is gagged, whenever that changes and every few seconds for players who just joined.
	/// The host never sees voice on the public engine, so each player's Superuser silences gagged speakers itself.
	/// </summary>
	internal void PublishVoiceGags( Action<string[]> publish )
	{
		var now = DateTimeOffset.UtcNow;
		if ( now < _nextGagCheck ) return;
		_nextGagCheck = now.AddSeconds( VoiceGagCheckSeconds );
		if ( !IsUsable || OnlinePlayers() is not { } players ) return;
		var gagged = players.Select( player => player.SteamId )
			.Where( steamId => Moderation.IsRestricted( steamId, Core.Moderation.ModerationKind.Gag ) ).OrderBy( id => id, StringComparer.Ordinal ).ToArray();
		if ( gagged.SequenceEqual( _publishedGags ) && now < _nextGagPublish ) return;
		_publishedGags = gagged;
		_nextGagPublish = now.AddSeconds( VoiceGagRefreshSeconds );
		publish( gagged );
	}
}
