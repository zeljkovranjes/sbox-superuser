namespace SboxSuperuser;

/// <summary>Exact-key policy entry; Unset removes an entry rather than granting access.</summary>
public enum PermissionEffect
{
	/// <summary>No explicit policy entry.</summary>
	Unset = 0,

	/// <summary>Explicit denial.</summary>
	Deny = 1,

	/// <summary>Explicit allowance, still subject to resolution and target protection.</summary>
	Allow = 2
}
