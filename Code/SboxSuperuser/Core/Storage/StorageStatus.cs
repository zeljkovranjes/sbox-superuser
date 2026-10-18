namespace SboxSuperuser.Core.Storage;

internal enum StorageStatus
{
	Unknown, Found, Missing, Committed, Corrupt, UnsupportedSchema,
	Unavailable, Cancelled, Conflict, InvalidData
}
