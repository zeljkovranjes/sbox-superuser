namespace SboxSuperuser.Core.Commands;

internal sealed record RequestKey( int ActorKind, string SubjectId, int Origin, Guid RequestId );
