using SboxSuperuser.Core.Prefixes;

namespace SboxSuperuser.Engine.Prefixes;

/// <summary>
/// What the Prefix module needs from the server, passed in so the module stays testable without the engine.
/// GroupsOf and Groups include the "owner" pseudo-group (protection 100), which stands for every owner. ApiGuide is the facade's own
/// documentation page, handed in because Engine code doesn't reach the facade. PermissionKnown is true for keys the
/// framework already registered (those in the default policy), which a module must not register again.
/// </summary>
internal sealed record PrefixHost(
	IAdminStore Store,
	Func<IReadOnlyList<string>> OnlinePlayers,
	Func<string, IReadOnlyList<GroupRank>> GroupsOf,
	Func<IReadOnlyList<GroupRank>> Groups,
	Func<string, string> CompileProblem,
	Func<string, bool> PermissionKnown,
	bool Chat,
	string ApiGuide = "" );
