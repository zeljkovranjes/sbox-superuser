namespace SboxSuperuser.Core.Storage;

internal sealed record StoragePruneResult( StorageStatus Status, int RemovedGenerations, bool HasMore );
