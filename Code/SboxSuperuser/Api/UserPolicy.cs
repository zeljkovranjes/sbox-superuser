using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser;

/// <summary>Immutable stored player groups and direct permissions, available for online and offline Steam IDs.</summary>
public sealed class UserPolicy
{
	internal UserDefinition Definition { get; }

	/// <summary>Gets the canonical decimal Steam ID.</summary>
	public string SteamId => Definition.SteamId;

	/// <summary>Gets explicitly assigned groups; every user additionally inherits player.</summary>
	public IReadOnlyList<string> Groups => Definition.Groups;

	/// <summary>Gets direct allow/deny entries, with Unset entries removed.</summary>
	public IReadOnlyList<PermissionEntry> Permissions { get; }

	/// <summary>Snapshots user policy without changing runtime authorization or granting owner membership.</summary>
	public UserPolicy( string steamId, IEnumerable<string> groups = null, IEnumerable<PermissionEntry> permissions = null )
	{
		var entries = ContractGuard.Copy( permissions ?? Array.Empty<PermissionEntry>(), nameof( permissions ) );
		Definition = new UserDefinition( steamId, groups,
			entries.Select( entry => new KeyValuePair<string, int>( entry.Key, (int)entry.Effect ) ) );
		Permissions = Array.AsReadOnly( entries.Where( entry => entry.Effect != PermissionEffect.Unset ).ToArray() );
	}
}
