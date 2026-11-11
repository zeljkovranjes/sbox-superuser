namespace SboxSuperuser;

/// <summary>An account in alt-account detection: a suspect on the dashboard, or one of an account's linked alts.</summary>
public sealed class AdminAltRow
{
	/// <summary>Gets the account's Steam ID.</summary>
	public string SteamId { get; init; } = "";

	/// <summary>Gets the name they last used here.</summary>
	public string Name { get; init; } = "";

	/// <summary>Gets whether they are connected now.</summary>
	public bool Online { get; init; }

	/// <summary>Gets whether they are banned now.</summary>
	public bool Banned { get; init; }

	/// <summary>Gets why they are linked, strongest first: Family share, Same network or Same party. Empty for suspects.</summary>
	public string[] Evidence { get; init; } = Array.Empty<string>();

	/// <summary>Gets when they last connected, in UTC.</summary>
	public DateTimeOffset LastSeenUtc { get; init; }

	/// <summary>Gets how risky the suspect looks; higher is riskier, and links to banned accounts weigh most.</summary>
	public int Score { get; init; }

	/// <summary>Gets how many accounts the suspect is linked to.</summary>
	public int AltCount { get; init; }

	/// <summary>Gets whether any linked account is banned, the usual sign of ban evasion.</summary>
	public bool LinkedToBanned { get; init; }
}
