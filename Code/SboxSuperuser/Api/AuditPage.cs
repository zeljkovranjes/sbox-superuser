namespace SboxSuperuser;

/// <summary>A permission-filtered bounded audit page with an explicit read outcome.</summary>
public sealed class AuditPage
{
	/// <summary>Gets Success, Denied, Conflict or a storage failure status.</summary>
	public CommandStatus Status { get; }
	/// <summary>Gets immutable authorized rows, newest sequence first.</summary>
	public IReadOnlyList<AuditEntry> Entries { get; }
	/// <summary>Gets the next page cursor, or null when no further matching rows exist.</summary>
	public AuditCursor NextCursor { get; }
	/// <summary>Gets the journal revision used for this response.</summary>
	public long Revision { get; }
	/// <summary>Gets a safe status message.</summary>
	public string Message { get; }

	internal AuditPage( CommandStatus status, string message, long revision = 0, IEnumerable<AuditEntry> entries = null, AuditCursor nextCursor = null )
	{
		Status = status;
		Message = message;
		Revision = revision;
		Entries = Array.AsReadOnly( (entries ?? Array.Empty<AuditEntry>()).ToArray() );
		NextCursor = nextCursor;
	}
}
