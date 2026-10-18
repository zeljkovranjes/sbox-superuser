namespace SboxSuperuser.Core.Storage;

internal sealed record StoredRevision( Guid GenerationId, int SchemaVersion, long Revision, string PayloadJson );
