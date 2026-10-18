namespace SboxSuperuser.Core.Permissions;

internal sealed record PolicyDecision( string Permission, bool IsAllowed, int DirectEntry,
	IReadOnlyList<PolicyContribution> Contributions, long PolicyRevision, long RegistryRevision, bool IsRegistered, string Reason );
