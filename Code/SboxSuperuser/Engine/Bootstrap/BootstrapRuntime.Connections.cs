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

	// Voice arrives many times a second per speaker; the Steam ID string is made once per connection.
	private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Connection, string> _voiceSenders = new();

	/// <summary>Whether the host should relay this sender's voice; gagged players and unusable state are refused.</summary>
	internal bool AllowsVoice( Connection sender )
	{
		if ( !IsUsable || sender is null || !sender.IsActive ) return false;
		var steamId = _voiceSenders.GetValue( sender, connection => connection.SteamId.ToString() );
		return !Moderation.IsRestricted( steamId, Core.Moderation.ModerationKind.Gag );
	}
}
