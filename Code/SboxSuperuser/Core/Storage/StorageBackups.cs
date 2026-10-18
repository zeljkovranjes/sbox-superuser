namespace SboxSuperuser.Core.Storage;

internal sealed record StorageBackups( StorageStatus Status, long HeadRevision,
	IReadOnlyList<StoredRevision> Documents, bool HasMore );
