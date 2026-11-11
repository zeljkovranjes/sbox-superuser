namespace SboxSuperuser.Engine.Players;

/// <summary>Changes the host asks a body's owner to make, since only the owner moves its own player.</summary>
internal enum BodyEffect
{
	Teleport,
	Push,
	Freeze,
	Unfreeze,
	Noclip,
	Unnoclip,
	Ragdoll,
	Unragdoll
}
