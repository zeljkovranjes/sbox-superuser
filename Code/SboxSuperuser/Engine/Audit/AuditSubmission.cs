namespace SboxSuperuser.Engine.Audit;

internal sealed class AuditSubmission
{
	internal AuditAction Action { get; set; }
	internal bool Handled { get; set; }
}
