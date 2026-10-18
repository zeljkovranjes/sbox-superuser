using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser;

/// <summary>Immutable named group policy; owner authority cannot be represented by an ordinary group.</summary>
public sealed class GroupPolicy
{
	internal GroupDefinition Definition { get; }

	/// <summary>Gets the stable lowercase group ID.</summary>
	public string Id => Definition.Id;

	/// <summary>Gets direct parent IDs; graph validation occurs when constructing the complete policy.</summary>
	public IReadOnlyList<string> Parents => Definition.Parents;

	/// <summary>Gets exact allow/deny entries, with Unset entries removed.</summary>
	public IReadOnlyList<PermissionEntry> Permissions { get; }

	/// <summary>Gets the group's nonnegative target-protection level.</summary>
	public int ProtectionLevel => Definition.ProtectionLevel;

	/// <summary>Snapshots group fields; no role is granted and no runtime policy is changed.</summary>
	public GroupPolicy( string id, IEnumerable<string> parents = null,
		IEnumerable<PermissionEntry> permissions = null, int protectionLevel = 0 )
	{
		var entries = ContractGuard.Copy( permissions ?? Array.Empty<PermissionEntry>(), nameof( permissions ) );
		Definition = new GroupDefinition( id, parents,
			entries.Select( entry => new KeyValuePair<string, int>( entry.Key, (int)entry.Effect ) ), protectionLevel );
		Permissions = Array.AsReadOnly( entries.Where( entry => entry.Effect != PermissionEffect.Unset ).ToArray() );
	}
}
