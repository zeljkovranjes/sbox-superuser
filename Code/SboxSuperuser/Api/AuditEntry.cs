using SboxSuperuser.Core.Audit;

namespace SboxSuperuser;

/// <summary>Immutable host-filtered action evidence containing only approved audit fields.</summary>
public sealed class AuditEntry
{
	private readonly AuditRecord _record;
	/// <summary>Gets the stable journal sequence used for pagination.</summary>
	public long Sequence => _record.Sequence;
	/// <summary>Gets the action identifier linking its intent and outcome.</summary>
	public Guid Id => _record.Id;
	/// <summary>Gets the originating request identifier.</summary>
	public Guid RequestId => _record.RequestId;
	/// <summary>Gets the recorded authenticated actor kind.</summary>
	public ActorKind ActorKind => Enum.Parse<ActorKind>( _record.ActorKind );
	/// <summary>Gets the authenticated actor's stable subject.</summary>
	public string ActorSubject => _record.ActorSubject;
	/// <summary>Gets the authenticated source.</summary>
	public CommandOrigin Origin => Enum.Parse<CommandOrigin>( _record.Origin );
	/// <summary>Gets the stable command key, never a full native command string.</summary>
	public string Command => _record.Command;
	/// <summary>Gets redaction markers or explicitly approved typed scalar values.</summary>
	public IReadOnlyDictionary<string, string> SafeArguments => _record.SafeArguments;
	/// <summary>Gets stable affected player IDs.</summary>
	public IReadOnlyList<string> Targets => _record.Targets;
	/// <summary>Gets when the action was recorded, in UTC.</summary>
	public DateTimeOffset CreatedUtc => _record.CreatedUtc;
	/// <summary>Gets the observed outcome timestamp; null means an unresolved intent.</summary>
	public DateTimeOffset? CompletedUtc => _record.CompletedUtc;
	/// <summary>Gets the recorded outcome; unresolved or interrupted work remains Unknown.</summary>
	public CommandStatus Outcome => Enum.Parse<CommandStatus>( _record.Outcome );

	internal AuditEntry( AuditRecord record )
	{
		_record = record;
	}
}
