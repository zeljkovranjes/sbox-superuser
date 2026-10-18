using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Audit;

internal interface IAuditRetention
{
	Task<StoragePruneResult> PruneAuditAsync( long expectedRevision, CancellationToken cancellation );
}
