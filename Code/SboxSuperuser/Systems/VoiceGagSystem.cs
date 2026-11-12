using Sandbox;

namespace SboxSuperuser.Systems;

/// <summary>
/// Silences gagged players on this machine by switching off their Voice components. On the public engine voice goes
/// from player to player without the host, so a gag holds only because every player's Superuser does this: the
/// gagged player's own copy stops sending, and everyone else's stops playing what arrives. A Voice switched off here
/// is switched back on when the gag ends; games that play voice without a Voice component can't be gagged.
/// </summary>
internal sealed class VoiceGagSystem : GameObjectSystem<VoiceGagSystem>
{
	private const float Interval = 0.5f;

	private readonly HashSet<string> _gagged = new( StringComparer.Ordinal );
	private readonly HashSet<Voice> _silenced = new();
	private readonly List<Voice> _released = new();
	// Steam IDs are compared as strings; each connection's is made once.
	private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Connection, string> _steamIds = new();
	private RealTimeSince _sinceCheck;

	public VoiceGagSystem( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 50, Enforce, nameof( VoiceGagSystem ) );

	/// <summary>Replaces who is gagged, as the host last said.</summary>
	internal void SetGagged( IReadOnlyList<string> steamIds )
	{
		_gagged.Clear();
		foreach ( var steamId in steamIds ) _gagged.Add( steamId );
		_sinceCheck = Interval;
	}

	private void Enforce()
	{
		if ( _sinceCheck < Interval ) return;
		_sinceCheck = 0;
		foreach ( var voice in Scene.GetAllComponents<Voice>() )
		{
			if ( !IsGagged( voice ) || !voice.Enabled ) continue;
			voice.Enabled = false;
			_silenced.Add( voice );
		}
		_released.Clear();
		foreach ( var voice in _silenced )
			if ( !voice.IsValid() || !IsGagged( voice ) ) _released.Add( voice );
		foreach ( var voice in _released )
		{
			_silenced.Remove( voice );
			if ( voice.IsValid() ) voice.Enabled = true;
		}
	}

	private bool IsGagged( Voice voice ) => _gagged.Count > 0 && voice.Network.Owner is { } owner
		&& _gagged.Contains( _steamIds.GetValue( owner, connection => connection.SteamId.ToString() ) );
}
