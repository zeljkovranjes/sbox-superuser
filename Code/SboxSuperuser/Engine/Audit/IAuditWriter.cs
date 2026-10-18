namespace SboxSuperuser.Engine.Audit;

internal interface IAuditWriter
{
	Task<bool> BeginAsync( AuditAction action, CancellationToken cancellation );
	Task<bool> CompleteAsync( AuditAction action, CommandStatus outcome, CancellationToken cancellation );
	Task<bool> RecordAsync( AuditAction action, CommandStatus outcome, CancellationToken cancellation );
}
