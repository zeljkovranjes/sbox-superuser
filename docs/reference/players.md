# Player actions

Commands such as `slay`, `freeze`, `bring` and `noclip` act on players' bodies.
Superuser does not know what a body is in your game, so it asks an
`IPlayerActions` to do the work after it has checked permission and target
protection. Every method takes exact Steam IDs and returns a `CommandResult`.

Without any setup, Superuser uses `PlayerControllerActions`, which works with
the engine's `PlayerController`:

| Works out of the box | How |
|---|---|
| `bring`, `goto`, `send`, `teleport`, `return`, `jailtp` | Moves the body on its owner's machine |
| `freeze`, `jail` | Pins the body in place; jail builds a solid cage around it |
| `slap`, `whip` | Knocks the body around |
| `slay`, `sslay`, `maul`, slap damage, `ignite` | Damage through the engine's `IDamageable` |
| `noclip` | A movement mode that flies through walls |
| `cloak`, `ragdoll`, `blind`, `spectate` | Hidden renderers, a physics ragdoll, a black screen, a borrowed camera |
| `playsound`, `map` | A sound for everyone; a new map in the scene's Map Instance |

Damage carries the tag `superuser.slay`, `superuser.slap`, or `fire` and `burn`
for ignite, so your health component can tell where it came from.

Health, armor and god mode belong to your game, and `strip` needs the engine's
inventory. Until you supply them, those commands answer **Unsupported** with an
explanation instead of doing nothing. Derive from `PlayerControllerActions`
and override what your game knows:

```csharp
public sealed class MyPlayerActions : PlayerControllerActions
{
	public override CommandResult SetHealth( string steamId, int health )
	{
		if ( FindHealth( steamId ) is not { } component ) return new( CommandStatus.NotFound, "No body." );
		component.Health = health;
		return new( CommandStatus.Success, "Done." );
	}
}

// Trusted host code, once on startup:
Superuser.ConfigurePlayerActions( new MyPlayerActions() );
```

A game without a `PlayerController` implements `IPlayerActions` directly.

`god`, `ungod`, `hp`, `armor`, `cloak` and `uncloak` work on yourself; aimed at
someone else they need higher protection, like every punishment.
