namespace SboxSuperuser.Core.Storage;

internal sealed record StorageOutcome( StorageStatus Status, StoredRevision Document = null );
