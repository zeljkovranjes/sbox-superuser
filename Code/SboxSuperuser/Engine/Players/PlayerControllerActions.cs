using Sandbox;
using SboxSuperuser.Engine.Players;

namespace SboxSuperuser;

/// <summary>
/// Player actions for games that use the engine's PlayerController. Damage, slay and ignite go through the engine's
/// IDamageable, so they work with any health component that implements it. Health, armor and god mode are the
/// game's own; override them, and anything else the game does differently, in a subclass passed to
/// Superuser.ConfigurePlayerActions.
/// </summary>
public class PlayerControllerActions : IPlayerActions
{
	private const float SlapSpeed = 400;
	private const float SlayDamage = 1_000_000_000f;
	private const float AimDistance = 4096;
	private const float BodyClearance = 32;
	private const float StandingDistance = 64;

	private readonly Dictionary<string, Vector3> _previousPositions = new();
	private readonly Dictionary<string, GameObject> _cages = new();
	private readonly Dictionary<string, GameObject> _fires = new();

	public virtual CommandResult Kill( string steamId, bool silent ) => Damage( steamId, SlayDamage, "superuser.slay" );

	public virtual CommandResult Slap( string steamId, float damage )
	{
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		var push = Vector3.Random.WithZ( 0 ).Normal * SlapSpeed + Vector3.Up * SlapSpeed;
		Superuser.ApplyBodyEffect( controller.Id, BodyEffect.Push, push );
		return damage > 0 ? Damage( steamId, damage, "superuser.slap" ) : Done();
	}

	public virtual CommandResult SetFrozen( string steamId, bool frozen ) =>
		Effect( steamId, frozen ? BodyEffect.Freeze : BodyEffect.Unfreeze );

	public virtual CommandResult SetBlind( string steamId, bool blind )
	{
		if ( PlayerBodies.Connection( steamId ) is not { } connection ) return new CommandResult( CommandStatus.NotFound, "That player is not connected." );
		using ( Rpc.FilterInclude( connection ) ) Superuser.SetBlindScreen( blind );
		return Done();
	}

	public virtual CommandResult SetIgnited( string steamId, bool ignited )
	{
		if ( _fires.Remove( steamId, out var fire ) && fire.IsValid() ) fire.Destroy();
		if ( !ignited ) return Done();
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		if ( !HasDamageable( controller ) ) return NoDamage();
		fire = controller.Scene.CreateObject();
		fire.Name = "Superuser Fire";
		fire.AddComponent<Burning>().Target = controller.GameObject;
		_fires[ steamId ] = fire;
		return Done();
	}

	public virtual CommandResult SetCloaked( string steamId, bool cloaked )
	{
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		Superuser.SetCloak( controller.Id, cloaked );
		return Done();
	}

	public virtual CommandResult SetGod( string steamId, bool god ) => Unsupported( "god mode" );

	public virtual CommandResult SetHealth( string steamId, int health ) => Unsupported( "health" );

	public virtual CommandResult SetArmor( string steamId, int armor ) => Unsupported( "armor" );

	public virtual CommandResult Strip( string steamId )
	{
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		var inventory = controller.GameObject.Root.GetComponentInChildren<BaseInventoryComponent>( true );
		if ( inventory is null ) return Unsupported( "an inventory" );
		foreach ( var item in inventory.GameObject.Root.GetComponentsInChildren<BaseInventoryItem>( true ).ToArray() ) inventory.Remove( item );
		return Done();
	}

	public virtual CommandResult SetRagdoll( string steamId, bool ragdoll ) =>
		Effect( steamId, ragdoll ? BodyEffect.Ragdoll : BodyEffect.Unragdoll, toEveryone: true );

	public virtual CommandResult SetNoclip( string steamId, bool noclip ) =>
		Effect( steamId, noclip ? BodyEffect.Noclip : BodyEffect.Unnoclip );

	public virtual CommandResult Spectate( string spectatorSteamId, string targetSteamId )
	{
		if ( PlayerBodies.Connection( spectatorSteamId ) is not { } spectator ) return new CommandResult( CommandStatus.NotFound, "You are not connected." );
		var target = Guid.Empty;
		if ( targetSteamId is not null )
		{
			if ( !PlayerBodies.TryFind( targetSteamId, out var controller, out var missing ) ) return missing;
			target = controller.Id;
		}
		using ( Rpc.FilterInclude( spectator ) ) Superuser.SpectateBody( target );
		return Done();
	}

	public virtual CommandResult MoveTo( string steamId, string destinationSteamId )
	{
		if ( !PlayerBodies.TryFind( destinationSteamId, out var destination, out var missing ) ) return missing;
		var facing = destination.EyeAngles.WithPitch( 0 ).Forward;
		return Teleport( steamId, destination.WorldPosition + facing * StandingDistance );
	}

	public virtual CommandResult MoveToAim( string steamId, string aimingSteamId )
	{
		if ( !PlayerBodies.TryFind( aimingSteamId, out var aiming, out var missing ) ) return missing;
		var start = aiming.EyePosition;
		var trace = aiming.Scene.Trace.Ray( start, start + aiming.EyeAngles.Forward * AimDistance )
			.IgnoreGameObjectHierarchy( aiming.GameObject.Root ).Run();
		if ( !trace.Hit ) return new CommandResult( CommandStatus.InvalidArguments, "Look at a surface to send them there." );
		// Step back from whatever was hit so the body doesn't end up inside it.
		return Teleport( steamId, trace.EndPosition + trace.Normal * BodyClearance );
	}

	public virtual CommandResult Return( string steamId )
	{
		if ( !_previousPositions.Remove( steamId, out var previous ) )
			return new CommandResult( CommandStatus.NoChange, "That player hasn't been moved since they spawned." );
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		Superuser.ApplyBodyEffect( controller.Id, BodyEffect.Teleport, previous );
		return Done();
	}

	public virtual CommandResult SetJailed( string steamId, bool jailed )
	{
		if ( _cages.Remove( steamId, out var cage ) && cage.IsValid() ) cage.Destroy();
		if ( !jailed ) return Done();
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		_cages[ steamId ] = JailCage.Build( controller.Scene, controller.WorldPosition, controller.BodyRadius, controller.BodyHeight );
		return Done();
	}

	public virtual CommandResult PlaySound( string sound )
	{
		Superuser.PlaySoundForEveryone( sound );
		return Done();
	}

	public virtual CommandResult ChangeMap( string map )
	{
		if ( Game.ActiveScene?.GetAllComponents<MapInstance>().Any() == true )
		{
			Superuser.ChangeMapForEveryone( map );
			return Done();
		}
		if ( Game.IsEditor ) return new CommandResult( CommandStatus.Unsupported, "This scene has no Map Instance to change; in the editor, stop play and change the scene." );
		LaunchArguments.Map = map;
		Game.Load( Game.Ident, true );
		return Done();
	}

	/// <summary>Moves a body and remembers where it was, for return.</summary>
	protected CommandResult Teleport( string steamId, Vector3 position )
	{
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		_previousPositions[ steamId ] = controller.WorldPosition;
		Superuser.ApplyBodyEffect( controller.Id, BodyEffect.Teleport, position );
		return Done();
	}

	/// <summary>Deals damage to every IDamageable on the player's root, tagged so games can tell it came from Superuser.</summary>
	protected CommandResult Damage( string steamId, float amount, string tag )
	{
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		if ( !HasDamageable( controller ) ) return NoDamage();
		var damage = new DamageInfo { Damage = amount, Position = controller.WorldPosition, Origin = controller.WorldPosition };
		damage.Tags.Add( tag );
		controller.GameObject.Root.RunEvent<Component.IDamageable>( listener => listener.OnDamage( damage ) );
		return Done();
	}

	private static CommandResult Effect( string steamId, BodyEffect effect, bool toEveryone = false )
	{
		if ( !PlayerBodies.TryFind( steamId, out var controller, out var missing ) ) return missing;
		if ( toEveryone ) Superuser.ApplyBodyEffect( controller.Id, effect, Vector3.Zero );
		else using ( Rpc.FilterInclude( controller.Network.Owner ) ) Superuser.ApplyBodyEffect( controller.Id, effect, Vector3.Zero );
		return Done();
	}

	private static bool HasDamageable( PlayerController controller ) =>
		controller.GameObject.Root.GetComponentsInChildren<Component.IDamageable>( true ).Any();

	private static CommandResult NoDamage() => new( CommandStatus.Unsupported,
		"This game's players don't take damage through the engine's IDamageable; override this action for your health system." );

	protected static CommandResult Unsupported( string what ) => new( CommandStatus.Unsupported,
		$"This game hasn't told Superuser about {what}; derive from PlayerControllerActions and pass it to Superuser.ConfigurePlayerActions." );

	protected static CommandResult Done() => new( CommandStatus.Success, "Done." );
}
