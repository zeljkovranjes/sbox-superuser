namespace SboxSuperuser.Engine.Authority;

internal sealed partial class HostPolicyService
{
	// Only called after the administration query has authorized a moderation reader.
	internal string[] ReadRoleLabels( string steamId ) => IsOwner( steamId ) ? new[] { "owner" }
		: _resolver.Policy.ApplicableGroups( steamId ).ToArray();
}
