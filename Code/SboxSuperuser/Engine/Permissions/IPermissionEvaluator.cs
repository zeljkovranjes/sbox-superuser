namespace SboxSuperuser.Engine.Permissions;

// T05 boundary: bridge host identity to engine-independent policy evaluation.
// Unknown permissions deny; this declaration supplies no default implementation.
internal interface IPermissionEvaluator
{
	PermissionExplanation Explain( ActorIdentity actor, string permission );
}
