namespace SboxSuperuser.Core.Permissions;

internal sealed record ProtectionDecision( bool IsAllowed, int ActorLevel, int TargetLevel, string Reason );
