using Sandbox;

namespace SboxSuperuser.Engine.Players;

/// <summary>
/// Burns a player from a host-owned object outside their body. FireDamage can't be used on the body itself:
/// anything inside a client-owned player is a proxy on the host, and FireDamage only runs where it isn't one.
/// </summary>
[Hide]
internal sealed class Burning : Component
{
	private const float DamagePerSecond = 20;
	private const float Interval = 0.2f;

	internal GameObject Target { get; set; }

	private TimeSince _sinceDamage;

	protected override void OnFixedUpdate()
	{
		if ( !Target.IsValid() ) { GameObject.Destroy(); return; }
		if ( _sinceDamage < Interval ) return;
		_sinceDamage = 0;
		// The same damage and tags FireDamage deals, so games treat it as fire.
		var damage = new DamageInfo { Damage = DamagePerSecond * Interval, Position = Target.WorldPosition, Origin = Target.WorldPosition };
		damage.Tags.Add( "fire" );
		damage.Tags.Add( "burn" );
		Target.Root.RunEvent<IDamageable>( listener => listener.OnDamage( damage ) );
	}
}
