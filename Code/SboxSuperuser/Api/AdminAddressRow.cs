namespace SboxSuperuser;

/// <summary>One IP address a player connected from; only sent to staff with admin.alts.ips.</summary>
public sealed class AdminAddressRow
{
	/// <summary>Gets the IP address, without a port.</summary>
	public string Address { get; init; } = "";

	/// <summary>Gets when they first connected from it, in UTC.</summary>
	public DateTimeOffset FirstSeenUtc { get; init; }

	/// <summary>Gets when they last connected from it, in UTC.</summary>
	public DateTimeOffset LastSeenUtc { get; init; }
}
