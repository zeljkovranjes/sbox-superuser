namespace SboxSuperuser;

/// <summary>A player who owns spawned objects, in the Props window.</summary>
public sealed class AdminPropOwnerRow
{
	/// <summary>Gets the owner's Steam ID.</summary>
	public string SteamId { get; init; } = "";

	/// <summary>Gets the owner's name while they are online, otherwise their Steam ID.</summary>
	public string Name { get; init; } = "";

	/// <summary>Gets whether they are connected now.</summary>
	public bool Online { get; init; }

	/// <summary>Gets how many objects they spawned that still exist.</summary>
	public int Props { get; init; }
}
